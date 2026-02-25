using UnityEngine;
using UnityEngine.InputSystem;

public static class PlayerInputReader
{
    private const float StickDeadzone = 0.2f;

    // Snapshot of input state for one frame
    public readonly struct PlayerInputState
    {
        public readonly Vector2 direction;
        public readonly bool thrustHeld;
        public readonly bool thrustPressed;
        public readonly bool thrustReleased;

        public PlayerInputState(Vector2 direction, bool thrustHeld, bool thrustPressed, bool thrustReleased)
        {
            this.direction = direction;
            this.thrustHeld = thrustHeld;
            this.thrustPressed = thrustPressed;
            this.thrustReleased = thrustReleased;
        }
    }

    // Read mouse, touch and gamepad input
    public static PlayerInputState Read(Transform playerTransform)
    {
        bool hasGamepad = Gamepad.current != null;
        Vector2 stick = hasGamepad ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        bool hasTouch = TryReadTouch(out Vector2 touchPosition, out bool touchHeld, out bool touchPressed, out bool touchReleased);

        Vector2 direction = GetDirection(playerTransform, stick, hasGamepad, hasTouch, touchPosition);

        bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseReleased = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

        bool gamepadHeld = hasGamepad && Gamepad.current.buttonSouth.isPressed;
        bool gamepadPressed = hasGamepad && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool gamepadReleased = hasGamepad && Gamepad.current.buttonSouth.wasReleasedThisFrame;

        return new PlayerInputState(
            direction,
            mouseHeld || gamepadHeld || touchHeld,
            mousePressed || gamepadPressed || touchPressed,
            mouseReleased || gamepadReleased || touchReleased
        );
    }

    // Choose stick direction when available, otherwise touch direction, otherwise mouse direction
    private static Vector2 GetDirection(
        Transform playerTransform,
        Vector2 stick,
        bool hasGamepad,
        bool hasTouch,
        Vector2 touchPosition
    )
    {
        if (hasGamepad && stick.magnitude > StickDeadzone)
            return stick.normalized;

        if (hasTouch && Camera.main != null)
        {
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(touchPosition);
            Vector2 touchDir = (Vector2)(touchPos - playerTransform.position);
            if (touchDir.sqrMagnitude > 0f)
                return touchDir.normalized;
        }

        if (Mouse.current == null || Camera.main == null)
            return playerTransform.up;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 mouseDir = (Vector2)(mousePos - playerTransform.position);

        return mouseDir.sqrMagnitude > 0f ? mouseDir.normalized : playerTransform.up;
    }

    private static bool TryReadTouch(
        out Vector2 touchPosition,
        out bool touchHeld,
        out bool touchPressed,
        out bool touchReleased
    )
    {
        touchPosition = Vector2.zero;
        touchHeld = false;
        touchPressed = false;
        touchReleased = false;

        var touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        var primaryTouch = touchscreen.primaryTouch;
        touchHeld = primaryTouch.press.isPressed;
        touchPressed = primaryTouch.press.wasPressedThisFrame;
        touchReleased = primaryTouch.press.wasReleasedThisFrame;

        if (touchHeld || touchPressed || touchReleased)
        {
            touchPosition = primaryTouch.position.ReadValue();
            return true;
        }

        foreach (var touch in touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            touchHeld = true;
            touchPosition = touch.position.ReadValue();
            return true;
        }

        return false;
    }
}
