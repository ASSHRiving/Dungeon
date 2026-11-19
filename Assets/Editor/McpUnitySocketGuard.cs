#if UNITY_EDITOR
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using McpUnity.Unity;
using McpUnity.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using WebSocketSharp.Server;
#endif

namespace Dungeon.EditorTools
{
    /// <summary>
    /// Guards the MCP Unity WebSocket server against Windows socket handle inheritance
    /// (child processes such as VS Code inheriting the listening/client sockets and locking
    /// the port across domain reloads) and automatically recovers if a transient port lock clears.
    /// </summary>
    [InitializeOnLoad]
    public static class McpUnitySocketGuard
    {
        private const string PortInUseErrorPrefix = "[MCP Unity] Failed to start WebSocket server: Port ";
        private const double PortRecoveryIntervalSeconds = 5.0d;

        private static readonly FieldInfo InstanceField =
            typeof(McpUnityServer).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);

        private static bool _orderedForCurrentSchedule;
        private static bool _delayCallHooked;
        private static bool _pendingPortRecovery;
        private static bool _loggedRecoveryNotice;
        private static double _nextPortRecoveryAttemptTime;

#if UNITY_EDITOR_WIN
        private const string McpServicePath = "/McpUnity";
        private const uint HandleFlagInherit = 0x00000001;

        private static readonly FieldInfo WebSocketServerField =
            typeof(McpUnityServer).GetField("_webSocketServer", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo ListenerField =
            typeof(WebSocketServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Type TcpContextType =
            typeof(WebSocketServer).Assembly.GetType("WebSocketSharp.Net.WebSockets.TcpListenerWebSocketContext");

        private static readonly FieldInfo ContextTcpClientField =
            TcpContextType?.GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);
#endif

        static McpUnitySocketGuard()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EnsureGuardOrderAndHarden();
        }

        private static void OnBeforeAssemblyReload()
        {
            _orderedForCurrentSchedule = false;
            _delayCallHooked = false;
            _pendingPortRecovery = false;
            _loggedRecoveryNotice = false;
        }

        private static void OnAfterAssemblyReload()
        {
            _orderedForCurrentSchedule = false;
            EnsureGuardOrderAndHarden();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                _orderedForCurrentSchedule = false;
                _delayCallHooked = false;
                _pendingPortRecovery = false;
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                EnsureGuardOrderAndHarden();
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error || string.IsNullOrEmpty(condition))
            {
                return;
            }

            if (condition.StartsWith(PortInUseErrorPrefix, StringComparison.Ordinal))
            {
                ArmPortRecovery();
            }
        }

        private static void OnDelayCall()
        {
            _delayCallHooked = false;
            EnsureGuardOrderAndHarden();
        }

        private static void OnEditorUpdate()
        {
            McpUnityServer server = GetExistingServerInstance();
            if (server == null)
            {
                return;
            }

            if (server.IsListening)
            {
                _pendingPortRecovery = false;
                _loggedRecoveryNotice = false;
                _orderedForCurrentSchedule = false;
                HardenServerSockets(server);
                return;
            }

            if (server.HasScheduledStart)
            {
                EnsureScheduledCallbacksOrder();
                return;
            }

            _orderedForCurrentSchedule = false;
            TryRecoverPortIfArmed(server);
        }

        private static void EnsureGuardOrderAndHarden()
        {
            McpUnityServer server = GetExistingServerInstance();
            if (server == null)
            {
                return;
            }

            if (server.IsListening)
            {
                _pendingPortRecovery = false;
                _loggedRecoveryNotice = false;
                HardenServerSockets(server);
            }
            else if (server.HasScheduledStart)
            {
                EnsureScheduledCallbacksOrder();
            }
        }

        private static void EnsureScheduledCallbacksOrder()
        {
            if (!_orderedForCurrentSchedule)
            {
                _orderedForCurrentSchedule = true;
                EditorApplication.update -= OnEditorUpdate;
                EditorApplication.update += OnEditorUpdate;
            }

            if (!_delayCallHooked)
            {
                _delayCallHooked = true;
                EditorApplication.delayCall -= OnDelayCall;
                EditorApplication.delayCall += OnDelayCall;
            }
        }

        private static McpUnityServer GetExistingServerInstance()
        {
            return InstanceField?.GetValue(null) as McpUnityServer;
        }

        private static void ArmPortRecovery()
        {
            _pendingPortRecovery = true;
            _nextPortRecoveryAttemptTime = EditorApplication.timeSinceStartup + PortRecoveryIntervalSeconds;

            if (!_loggedRecoveryNotice)
            {
                _loggedRecoveryNotice = true;
                McpLogger.LogWarning(
                    "Port is currently held by another process (for example, an IDE or child process launched before socket guard was active). " +
                    "Auto-recovery will restart the MCP server automatically once the port is released.");
            }
        }

        private static void TryRecoverPortIfArmed(McpUnityServer server)
        {
            if (!_pendingPortRecovery || EditorApplication.isCompiling || McpUtils.IsMultiplayerPlayModeClone())
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < _nextPortRecoveryAttemptTime)
            {
                return;
            }

            _nextPortRecoveryAttemptTime = now + PortRecoveryIntervalSeconds;

            McpUnitySettings settings = McpUnitySettings.Instance;
            if (!settings.AutoStartServer)
            {
                _pendingPortRecovery = false;
                return;
            }

            if (IsPortFreeForServer(settings.Port, settings.AllowRemoteConnections))
            {
                _pendingPortRecovery = false;
                _loggedRecoveryNotice = false;
                server.StartServer();
            }
        }

        private static bool IsPortFreeForServer(int port, bool allowRemoteConnections)
        {
            IPAddress bindAddress = ResolveBindAddress(allowRemoteConnections);
            if (bindAddress == null)
            {
                return false;
            }

            try
            {
                using (var probeSocket = new Socket(bindAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    HardenSocket(probeSocket);
                    probeSocket.Bind(new IPEndPoint(bindAddress, port));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static IPAddress ResolveBindAddress(bool allowRemoteConnections)
        {
            string host = allowRemoteConnections ? "0.0.0.0" : "localhost";
            if (IPAddress.TryParse(host, out IPAddress parsed))
            {
                return parsed;
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                return addresses.Length > 0 ? addresses[0] : IPAddress.Loopback;
            }
            catch
            {
                return IPAddress.Loopback;
            }
        }

        private static void HardenServerSockets(McpUnityServer server)
        {
#if UNITY_EDITOR_WIN
            if (!(WebSocketServerField?.GetValue(server) is WebSocketServer webSocketServer))
            {
                return;
            }

            HardenListenerSocket(webSocketServer);
            HardenClientSessionSockets(webSocketServer);
#endif
        }

#if UNITY_EDITOR_WIN
        private static void HardenListenerSocket(WebSocketServer webSocketServer)
        {
            if (!(ListenerField?.GetValue(webSocketServer) is TcpListener listener))
            {
                return;
            }

            try
            {
                HardenSocket(listener.Server);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static void HardenClientSessionSockets(WebSocketServer webSocketServer)
        {
            if (webSocketServer.WebSocketServices == null)
            {
                return;
            }

            if (!webSocketServer.WebSocketServices.TryGetServiceHost(McpServicePath, out WebSocketServiceHost host) || host?.Sessions == null)
            {
                return;
            }

            if (host.Sessions.Count == 0)
            {
                return;
            }

            foreach (IWebSocketSession session in host.Sessions.Sessions)
            {
                object context = session?.Context;
                if (context != null && TcpContextType != null && TcpContextType.IsInstanceOfType(context))
                {
                    try
                    {
                        var tcpClient = ContextTcpClientField?.GetValue(context) as TcpClient;
                        HardenSocket(tcpClient?.Client);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }
#endif

        private static void HardenSocket(Socket socket)
        {
#if UNITY_EDITOR_WIN
            if (socket == null)
            {
                return;
            }

            try
            {
                IntPtr handle = socket.Handle;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                if (GetHandleInformation(handle, out uint flags) && (flags & HandleFlagInherit) != 0)
                {
                    SetHandleInformation(handle, HandleFlagInherit, 0);
                }
            }
            catch (ObjectDisposedException)
            {
            }
#endif
        }
    }
}
#endif
