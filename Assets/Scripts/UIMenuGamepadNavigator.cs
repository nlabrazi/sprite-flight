using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public sealed class UIMenuGamepadNavigator
{
    private const string SelectedClassName = "is-hovered";
    private const float AxisDeadzone = 0.5f;
    private const float FirstRepeatDelay = 0.32f;
    private const float RepeatDelay = 0.12f;

    private readonly List<Button> buttons = new();

    private int selectedIndex = -1;
    private bool axisHeld;
    private float nextRepeatTime;

    public Button CurrentButton
    {
        get
        {
            if (selectedIndex < 0 || selectedIndex >= buttons.Count)
                return null;

            return buttons[selectedIndex];
        }
    }

    public void SetButtons(IEnumerable<Button> sourceButtons, bool resetSelection = false)
    {
        Button previousSelection = resetSelection ? null : CurrentButton;

        ClearSelectionVisuals();
        buttons.Clear();

        if (sourceButtons != null)
        {
            foreach (var button in sourceButtons)
            {
                if (button != null && button.enabledInHierarchy)
                {
                    // Prevent UI Toolkit submit from triggering an extra click on focused buttons.
                    button.focusable = false;
                    buttons.Add(button);
                }
            }
        }

        if (buttons.Count == 0)
        {
            selectedIndex = -1;
            axisHeld = false;
            return;
        }

        if (resetSelection || previousSelection == null)
        {
            selectedIndex = 0;
        }
        else
        {
            int keptIndex = buttons.IndexOf(previousSelection);
            selectedIndex = keptIndex >= 0 ? keptIndex : 0;
        }

        ApplySelectionVisual();
    }

    public void Clear()
    {
        ClearSelectionVisuals();
        buttons.Clear();
        selectedIndex = -1;
        axisHeld = false;
    }

    public void TickNavigation()
    {
        if (buttons.Count == 0)
            return;

        Vector2 axis = ReadNavigationVector();
        if (axis.magnitude < AxisDeadzone)
        {
            axisHeld = false;
            return;
        }

        bool verticalNavigation = Mathf.Abs(axis.y) >= Mathf.Abs(axis.x);
        int direction = verticalNavigation
            ? (axis.y > 0f ? -1 : 1)
            : (axis.x > 0f ? 1 : -1);

        float now = Time.unscaledTime;
        if (!axisHeld)
        {
            MoveSelection(direction);
            axisHeld = true;
            nextRepeatTime = now + FirstRepeatDelay;
            return;
        }

        if (now >= nextRepeatTime)
        {
            MoveSelection(direction);
            nextRepeatTime = now + RepeatDelay;
        }
    }

    public static bool WasSubmitPressedThisFrame()
    {
        bool gamepadSubmit = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool keyboardSubmit = Keyboard.current != null
            && (Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame);

        return gamepadSubmit || keyboardSubmit;
    }

    public static bool WasCancelPressedThisFrame()
    {
        bool gamepadCancel = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        bool keyboardCancel = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        return gamepadCancel || keyboardCancel;
    }

    private void MoveSelection(int direction)
    {
        if (buttons.Count == 0)
            return;

        selectedIndex = (selectedIndex + direction) % buttons.Count;
        if (selectedIndex < 0)
            selectedIndex += buttons.Count;

        ApplySelectionVisual();
    }

    private void ApplySelectionVisual()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            if (button == null)
                continue;

            if (i == selectedIndex)
            {
                button.AddToClassList(SelectedClassName);
            }
            else
            {
                button.RemoveFromClassList(SelectedClassName);
            }
        }
    }

    private void ClearSelectionVisuals()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            if (button != null)
                button.RemoveFromClassList(SelectedClassName);
        }
    }

    private static Vector2 ReadNavigationVector()
    {
        Vector2 axis = Vector2.zero;

        if (Gamepad.current != null)
        {
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            Vector2 stick = Gamepad.current.leftStick.ReadValue();

            if (dpad.sqrMagnitude > axis.sqrMagnitude)
                axis = dpad;

            if (stick.sqrMagnitude > axis.sqrMagnitude)
                axis = stick;
        }

        if (Keyboard.current != null)
        {
            float keyboardY = 0f;
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) keyboardY += 1f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) keyboardY -= 1f;

            float keyboardX = 0f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) keyboardX += 1f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) keyboardX -= 1f;

            Vector2 keyboardAxis = new Vector2(keyboardX, keyboardY);
            if (keyboardAxis.sqrMagnitude > axis.sqrMagnitude)
                axis = keyboardAxis;
        }

        return axis;
    }
}
