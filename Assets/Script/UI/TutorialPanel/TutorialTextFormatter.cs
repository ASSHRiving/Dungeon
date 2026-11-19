using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// 教學文案動態按鍵佔位符解析工具。
/// </summary>
public static class TutorialTextFormatter
{
    public static string FormatStepDescription(TutorialStepSO step, PlayerInput playerInput, InputActionAsset fallbackAsset)
    {
        if (step == null || string.IsNullOrEmpty(step.descriptionTemplate)) return string.Empty;

        string result = step.descriptionTemplate;
        if (step.actionBindings == null || step.actionBindings.Count == 0) return result;

        InputActionAsset asset = (playerInput != null && playerInput.actions != null)
            ? playerInput.actions
            : fallbackAsset;

        for (int i = 0; i < step.actionBindings.Count; i++)
        {
            TutorialActionPlaceholder binding = step.actionBindings[i];
            if (binding == null || string.IsNullOrEmpty(binding.placeholder)) continue;

            string keyLabel = ResolveActionKeyLabel(asset, binding);
            result = result.Replace(binding.placeholder, $"<color=#FCECA6><b>[{keyLabel}]</b></color>");
        }

        return result;
    }

    private static string ResolveActionKeyLabel(InputActionAsset asset, TutorialActionPlaceholder binding)
    {
        string fallback = binding.placeholder.Trim('{', '}');
        InputAction action = asset != null && !string.IsNullOrEmpty(binding.actionPath)
            ? asset.FindAction(binding.actionPath)
            : null;

        if (action == null) return fallback;

        if (binding.isCompositeGroup)
        {
            List<string> parts = new List<string>(4);
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding b = action.bindings[i];
                if (!b.isPartOfComposite) continue;

                string readable = InputControlPath.ToHumanReadableString(
                    b.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
                if (!string.IsNullOrEmpty(readable))
                {
                    parts.Add(FormatReadableKeyName(readable));
                }
            }

            if (parts.Count == 4 && parts[0] == "W" && parts[1] == "S" && parts[2] == "A" && parts[3] == "D")
            {
                return "WASD";
            }
            if (parts.Count > 0) return string.Join("/", parts);
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isComposite) continue;

            string readable = InputControlPath.ToHumanReadableString(
                b.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
            if (!string.IsNullOrEmpty(readable))
            {
                return FormatReadableKeyName(readable);
            }
        }

        return fallback;
    }

    private static string FormatReadableKeyName(string raw)
    {
        return raw switch
        {
            "Left Button" => "滑鼠左鍵",
            "Right Button" => "滑鼠右鍵",
            "Middle Button" => "滑鼠中鍵",
            "Left Shift" => "Shift",
            "Right Shift" => "R-Shift",
            "Space" => "空白鍵",
            "Up Arrow" => "↑",
            "Down Arrow" => "↓",
            "Left Arrow" => "←",
            "Right Arrow" => "→",
            _ => raw ?? string.Empty
        };
    }
}
