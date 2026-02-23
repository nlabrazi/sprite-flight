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

    // Read mouse and gamepad input
    public static PlayerInputState Read(Transform playerTransform)
    {
        bool hasGamepad = Gamepad.current != null;
        Vector2 stick = hasGamepad ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;

        Vector2 direction = GetDirection(playerTransform, stick, hasGamepad);

        bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseReleased = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

        bool gamepadHeld = hasGamepad && Gamepad.current.buttonSouth.isPressed;
        bool gamepadPressed = hasGamepad && Gamepad.current.buttonSouth.wasPressedThisFrame;
        bool gamepadReleased = hasGamepad && Gamepad.current.buttonSouth.wasReleasedThisFrame;

        return new PlayerInputState(
            direction,
            mouseHeld || gamepadHeld,
            mousePressed || gamepadPressed,
            mouseReleased || gamepadReleased
        );
    }

    // Choose stick direction when available, otherwise mouse direction
    private static Vector2 GetDirection(Transform playerTransform, Vector2 stick, bool hasGamepad)
    {
        if (hasGamepad && stick.magnitude > StickDeadzone)
            return stick.normalized;

        if (Mouse.current == null || Camera.main == null)
            return playerTransform.up;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = mousePos - playerTransform.position;
        return dir.sqrMagnitude > 0f ? dir.normalized : playerTransform.up;
    }
}
