using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float thrustForce = 1f;
    public float maxSpeed = 5f;

    [Header("Visual")]
    public GameObject rocketFlame;
    public GameObject explosionEffect;
    public GameObject borderParent;
    private Button restartButton;

    [Header("Audio")]
    public AudioClip explosionClip;
    public AudioClip thrustClip;                // son court (ex: sfx-SpaceshipEngineLight)
    [Range(0f, 1f)] public float thrustVolume = 0.35f;

    // 2 sources séparées : une pour l'explosion, une pour le thrust
    private AudioSource explosionSource;
    private AudioSource thrustSfxSource;

    [Header("Score")]
    public float scoreMultiplier = 10f;
    public UIDocument uiDocument;
    private Label highScoreText;
    private const string HIGH_SCORE_KEY = "HIGH_SCORE";

    private Rigidbody2D rb;
    private Label scoreText;

    private float elapsedTime;
    private float score;
    private bool isDead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // ================= UI =================
        var root = uiDocument.rootVisualElement;

        scoreText = root.Q<Label>("ScoreText");
        highScoreText = root.Q<Label>("HighScoreText");
        restartButton = root.Q<Button>("RestartButton");

        if (restartButton != null)
        {
            restartButton.style.display = DisplayStyle.None;
            restartButton.clicked += ReloadScene;
        }

        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        if (highScoreText != null)
            highScoreText.text = $"High Score: {highScore}";

        // ================= AUDIO =================
        explosionSource = gameObject.AddComponent<AudioSource>();
        explosionSource.playOnAwake = false;
        explosionSource.loop = false;
        explosionSource.spatialBlend = 0f; // 2D

        thrustSfxSource = gameObject.AddComponent<AudioSource>();
        thrustSfxSource.playOnAwake = false;
        thrustSfxSource.loop = false;
        thrustSfxSource.spatialBlend = 0f; // 2D
    }

    void Update()
    {
        UpdateScore();
        MoveRocket();
    }

    void UpdateScore()
    {
        elapsedTime += Time.deltaTime;
        score = Mathf.FloorToInt(elapsedTime * scoreMultiplier);

        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    void MoveRocket()
    {
        if (rb == null || isDead)
            return;

        bool hasGamepad = Gamepad.current != null;
        Vector2 stick = hasGamepad ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;

        Vector2 direction = GetDirection(stick, hasGamepad);

        bool thrustHeld =
            (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
            (hasGamepad && Gamepad.current.buttonSouth.isPressed);

        bool thrustPressed =
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (hasGamepad && Gamepad.current.buttonSouth.wasPressedThisFrame);

        bool thrustReleased =
            (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) ||
            (hasGamepad && Gamepad.current.buttonSouth.wasReleasedThisFrame);

        if (thrustHeld)
        {
            transform.up = direction;
            rb.AddForce(direction * thrustForce);

            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // --- SON COURT : joué à l'appui, coupé au relâche (pour éviter qu'il continue pendant la glisse)
        if (thrustPressed && thrustClip != null && thrustSfxSource != null)
        {
            thrustSfxSource.PlayOneShot(thrustClip, thrustVolume);
        }

        if (thrustReleased && thrustSfxSource != null)
        {
            thrustSfxSource.Stop();
        }

        if (rocketFlame != null)
        {
            if (thrustPressed) rocketFlame.SetActive(true);
            else if (thrustReleased) rocketFlame.SetActive(false);
        }
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    Vector2 GetDirection(Vector2 stick, bool hasGamepad)
    {
        const float deadzone = 0.2f;

        if (hasGamepad && stick.magnitude > deadzone)
            return stick.normalized;

        if (Mouse.current == null || Camera.main == null)
            return transform.up;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        return (mousePos - transform.position).normalized;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        isDead = true;

        // coupe immédiatement le son de thrust
        if (thrustSfxSource != null)
            thrustSfxSource.Stop();

        if (borderParent != null)
            borderParent.SetActive(false);

        int currentHigh = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        int currentScore = Mathf.FloorToInt(score);

        if (currentScore > currentHigh)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, currentScore);
            PlayerPrefs.Save();

            if (highScoreText != null)
                highScoreText.text = $"High Score: {currentScore}";
        }

        // Son explosion (indépendant du thrust)
        if (explosionClip != null && explosionSource != null)
            explosionSource.PlayOneShot(explosionClip);

        // Explosion visuelle
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        // UI
        if (restartButton != null)
            restartButton.style.display = DisplayStyle.Flex;

        // Stop gameplay
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        if (rb != null)
            rb.simulated = false;

        Destroy(gameObject, 1f);
    }
}