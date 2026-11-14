using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Unity.Cinemachine;

namespace Dungeon.CameraControl
{
    /// <summary>
    /// 控制 Cinemachine 相機遠近（滑鼠滾輪縮放）的專用組件。
    /// 支援 Cinemachine 3.x，具備平滑緩衝阻尼、距離邊界限制、UI 遮擋防誤觸。
    /// </summary>
    [AddComponentMenu("Cinemachine/Cinemachine Camera Zoom")]
    public class CinemachineCameraZoom : MonoBehaviour
    {
        public enum ZoomMode
        {
            AutoDetect,         // 自動偵測（優先使用 OrbitalFollow / ThirdPersonFollow）
            OrbitalFollowRadius,// 控制 OrbitalFollow 的 Radius
            ThirdPersonDistance,// 控制 ThirdPersonFollow 的 CameraDistance
            FollowOffset,       // 控制 Follow 的 FollowOffset 距離
            FieldOfView         // 控制 Lens 的視野角度 (FOV)
        }

        [Header("縮放模式")]
        [Tooltip("選擇要控制的相機屬性，預設 AutoDetect 會自動抓取物件上的組件")]
        [SerializeField] private ZoomMode zoomMode = ZoomMode.AutoDetect;

        [Header("Cinemachine 組件 (若為空則自動抓取)")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
        [SerializeField] private CinemachineFollow follow;

        [Header("距離 / 範圍設定")]
        [Tooltip("最近距離 (或最小 FOV)")]
        [SerializeField] private float minDistance = 1.0f;

        [Tooltip("最遠距離 (或最大 FOV)")]
        [SerializeField] private float maxDistance = 6.0f;

        [Header("縮放手感設定")]
        [Tooltip("滾動一次滾輪的縮放幅度")]
        [SerializeField] private float zoomStep = 0.4f;

        [Tooltip("縮放平滑插值速度 (數值越大越快到位，建議 8 ~ 15)")]
        [SerializeField] private float smoothSpeed = 12f;

        [Tooltip("反轉滾輪方向 (預設向前滾為拉近，向後滾為拉遠)")]
        [SerializeField] private bool invertScroll = false;

        [Header("防誤觸設定")]
        [Tooltip("當滑鼠游標位於 UI 元素上方時，忽略滾輪縮放")]
        [SerializeField] private bool ignoreWhenPointerOverUI = true;

        private float targetDistance;
        private float currentDistance;
        private ZoomMode activeMode;

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            // 啟用時同步目前實際距離
            SyncCurrentDistance();
        }

        private void Reset()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (cinemachineCamera == null) cinemachineCamera = GetComponent<CinemachineCamera>();
            if (orbitalFollow == null) orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            if (thirdPersonFollow == null) thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();
            if (follow == null) follow = GetComponent<CinemachineFollow>();

            // 決定運行時模式
            activeMode = zoomMode;
            if (activeMode == ZoomMode.AutoDetect)
            {
                if (orbitalFollow != null)
                {
                    activeMode = ZoomMode.OrbitalFollowRadius;
                }
                else if (thirdPersonFollow != null)
                {
                    activeMode = ZoomMode.ThirdPersonDistance;
                }
                else if (follow != null)
                {
                    activeMode = ZoomMode.FollowOffset;
                }
                else
                {
                    activeMode = ZoomMode.FieldOfView;
                }
            }

            SyncCurrentDistance();
        }

        private void SyncCurrentDistance()
        {
            switch (activeMode)
            {
                case ZoomMode.OrbitalFollowRadius:
                    if (orbitalFollow != null)
                    {
                        currentDistance = orbitalFollow.Radius;
                        targetDistance = currentDistance;
                    }
                    break;

                case ZoomMode.ThirdPersonDistance:
                    if (thirdPersonFollow != null)
                    {
                        currentDistance = thirdPersonFollow.CameraDistance;
                        targetDistance = currentDistance;
                    }
                    break;

                case ZoomMode.FollowOffset:
                    if (follow != null)
                    {
                        currentDistance = follow.FollowOffset.magnitude;
                        targetDistance = currentDistance;
                    }
                    break;

                case ZoomMode.FieldOfView:
                    if (cinemachineCamera != null)
                    {
                        currentDistance = cinemachineCamera.Lens.FieldOfView;
                        targetDistance = currentDistance;
                    }
                    break;
            }
        }

        private void Update()
        {
            // UI 遮擋防誤觸
            if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            float scrollDelta = GetScrollInput();

            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                // 向上滾通常為正值 (拉近)，向下滾為負值 (拉遠)
                float direction = invertScroll ? scrollDelta : -scrollDelta;
                targetDistance += direction * zoomStep;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }

            // 平滑插值更新相機
            if (Mathf.Abs(currentDistance - targetDistance) > 0.001f)
            {
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, smoothSpeed * Time.deltaTime);
                ApplyDistance(currentDistance);
            }
        }

        private void ApplyDistance(float distance)
        {
            switch (activeMode)
            {
                case ZoomMode.OrbitalFollowRadius:
                    if (orbitalFollow != null)
                    {
                        orbitalFollow.Radius = distance;
                    }
                    break;

                case ZoomMode.ThirdPersonDistance:
                    if (thirdPersonFollow != null)
                    {
                        thirdPersonFollow.CameraDistance = distance;
                    }
                    break;

                case ZoomMode.FollowOffset:
                    if (follow != null)
                    {
                        Vector3 currentOffset = follow.FollowOffset;
                        if (currentOffset.sqrMagnitude > 0.0001f)
                        {
                            follow.FollowOffset = currentOffset.normalized * distance;
                        }
                    }
                    break;

                case ZoomMode.FieldOfView:
                    if (cinemachineCamera != null)
                    {
                        var lens = cinemachineCamera.Lens;
                        lens.FieldOfView = distance;
                        cinemachineCamera.Lens = lens;
                    }
                    break;
            }
        }

        /// <summary>
        /// 獲取滑鼠滾輪輸入 (自動相容 New Input System 與 Legacy Input)
        /// </summary>
        private float GetScrollInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    // 將滾輪滾動量標準化為 +-1
                    return Mathf.Sign(scrollY);
                }
            }
#endif
            float legacyScroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(legacyScroll) > 0.01f)
            {
                return Mathf.Sign(legacyScroll);
            }

            return 0f;
        }

        /// <summary>
        /// 外部手動設定目標距離
        /// </summary>
        public void SetTargetDistance(float distance)
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        /// <summary>
        /// 重置距離
        /// </summary>
        public void ResetToDefault(float distance)
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentDistance = targetDistance;
            ApplyDistance(currentDistance);
        }
    }
}

