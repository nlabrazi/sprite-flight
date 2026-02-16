using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float thrustForce = 1f;
    public float maxSpeed = 5f;
    public GameObject rocketFlame;
    Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // 1) Lire le stick de la manette (si une manette existe)
        Vector2 stick = Vector2.zero;
        bool hasGamepad = Gamepad.current != null;
        if (hasGamepad)
            stick = Gamepad.current.leftStick.ReadValue();

        // 2) Si le stick bouge un peu, on l’utilise comme direction
        float deadzone = 0.2f; // évite le tremblement du stick
        bool stickActive = stick.magnitude > deadzone;

        Vector2 direction;

        if (stickActive)
        {
            direction = stick.normalized;
        }
        else
        {
            // Sinon on garde la souris (comme avant)
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
            direction = (mousePos - transform.position).normalized;
        }

        // 3) Détecter la poussée :
        // - souris clic gauche
        // - OU manette bouton A
        bool thrustHeld = Mouse.current.leftButton.isPressed
                          || (hasGamepad && Gamepad.current.buttonSouth.isPressed);

        bool thrustPressed = Mouse.current.leftButton.wasPressedThisFrame
                             || (hasGamepad && Gamepad.current.buttonSouth.wasPressedThisFrame);

        bool thrustReleased = Mouse.current.leftButton.wasReleasedThisFrame
                              || (hasGamepad && Gamepad.current.buttonSouth.wasReleasedThisFrame);

        // 4) Appliquer la poussée + rotation si on pousse
        if (thrustHeld)
        {
            transform.up = direction;
            rb.AddForce(direction * thrustForce);

            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        // 5) Flamme ON/OFF
        if (thrustPressed) rocketFlame.SetActive(true);
        else if (thrustReleased) rocketFlame.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }
}
