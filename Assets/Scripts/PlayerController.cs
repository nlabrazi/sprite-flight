using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Movement")]
    [SerializeField] private float thrustForce = 1f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Audio")]
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip thrustClip;
    [SerializeField, Range(0f, 1f)] private float thrustVolume = 0.35f;

    [Header("Visual")]
    [SerializeField] private GameObject rocketFlame;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private GameObject borderParent;

    [Header("Score")]
    [SerializeField] private float scoreMultiplier = 10f;

    // Core components
    private Rigidbody2D rb;
    private PlayerGameUI gameUI;

    // Audio sources
    private AudioSource explosionSource;
    private AudioSource thrustSource;

    // Runtime state
    private float elapsedTime;
    private int currentScore;
    private bool isDead;
    private bool isPaused;

    // Legacy high score key
    private const string HIGH_SCORE_KEY = "HIGH_SCORE";

    private void Awake()
    {
        Time.timeScale = 1f;
        rb = GetComponent<Rigidbody2D>();
        SetupAudioSources();
        SetupUI();

        int bestScore = GetBestStoredScore();
        gameUI?.Initialize(bestScore);
    }

    private void OnDestroy()
    {
        gameUI?.UnbindEvents(ResumeGameFromButton, ReloadScene, GoToMainMenu, SaveTop10, SkipTop10);
    }

    private void Update()
    {
        if (isDead)
        {
            gameUI?.TickNavigation(true);
            return;
        }

        if (WasPausePressedThisFrame()) TogglePause();
        if (isPaused)
        {
            gameUI?.TickNavigation(true);
            return;
        }

        gameUI?.TickNavigation(false);
        UpdateScore();
        MoveRocket();
    }

    // Update score from survival time
    private void UpdateScore()
    {
        elapsedTime += Time.deltaTime;
        currentScore = Mathf.FloorToInt(elapsedTime * scoreMultiplier);
        gameUI?.SetScore(currentScore);
    }

    // Move and orient the ship from player input
    private void MoveRocket()
    {
        if (rb == null) return;

        var input = PlayerInputReader.Read(transform);

        if (input.thrustHeld)
        {
            transform.up = input.direction;
            rb.AddForce(input.direction * thrustForce);

            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        if (input.thrustPressed && thrustClip != null)
            thrustSource.PlayOneShot(thrustClip, thrustVolume);

        if (input.thrustReleased)
            thrustSource.Stop();

        UpdateFlameState(input.thrustPressed, input.thrustReleased);
    }

    // Handle player death flow
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        isDead = true;

        // Stop thrust audio + flame
        thrustSource.Stop();
        if (rocketFlame != null) rocketFlame.SetActive(false);

        if (borderParent != null) borderParent.SetActive(false);

        int legacyHigh = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        if (currentScore > legacyHigh)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, currentScore);
            PlayerPrefs.Save();
        }

        if (explosionClip != null) explosionSource.PlayOneShot(explosionClip);
        if (explosionEffect != null) Instantiate(explosionEffect, transform.position, transform.rotation);

        if (rb != null) rb.simulated = false;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        bool qualifies = ScoreboardManager.WouldQualify(currentScore);

        if (qualifies)
        {
            gameUI?.ShowGameOverButtons(false);
            gameUI?.SetHudVisible(false);
            gameUI?.ShowNameEntry("AAA");
        }
        else
        {
            gameUI?.HideNameEntry();
            gameUI?.ShowGameOverButtons(true);
        }

        gameUI?.SetHighScore(GetBestStoredScore());
    }

    // Reload the current scene
    private void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Return to the main menu scene
    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Save score and return to menu
    private void SaveTop10()
    {
        Time.timeScale = 1f;
        string name = gameUI != null ? gameUI.GetPlayerNameOrDefault("AAA") : "AAA";
        ScoreboardManager.AddScore(name, currentScore);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Skip save and show post-death actions
    private void SkipTop10()
    {
        gameUI?.HideNameEntry();
        gameUI?.SetHudVisible(true);
        gameUI?.ShowGameOverButtons(true);
    }

    // Create and configure audio sources
    private void SetupAudioSources()
    {
        explosionSource = gameObject.AddComponent<AudioSource>();
        explosionSource.playOnAwake = false;
        explosionSource.loop = false;
        explosionSource.spatialBlend = 0f;

        thrustSource = gameObject.AddComponent<AudioSource>();
        thrustSource.playOnAwake = false;
        thrustSource.loop = false;
        thrustSource.spatialBlend = 0f;
    }

    // Create UI wrapper and bind actions
    private void SetupUI()
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        gameUI = new PlayerGameUI(root);
        gameUI.BindEvents(ResumeGameFromButton, ReloadScene, GoToMainMenu, SaveTop10, SkipTop10);
    }

    // Update rocket flame state from thrust events
    private void UpdateFlameState(bool thrustPressed, bool thrustReleased)
    {
        if (rocketFlame == null) return;

        if (thrustPressed) rocketFlame.SetActive(true);
        else if (thrustReleased) rocketFlame.SetActive(false);
    }

    // Read best score from both storage systems
    private int GetBestStoredScore()
    {
        int legacy = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        int top10 = ScoreboardManager.GetBestScore();
        return Mathf.Max(legacy, top10);
    }

    // Check pause input from gamepad start or keyboard escape
    private static bool WasPausePressedThisFrame()
    {
        bool gamepadPause = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        bool keyboardPause = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool touchPause = WasMobilePausePressedThisFrame();
        return gamepadPause || keyboardPause || touchPause;
    }

    // Pause on two-finger touch start for mobile/web touch devices
    private static bool WasMobilePausePressedThisFrame()
    {
        if (Touchscreen.current == null)
            return false;

        int activeTouches = 0;
        bool startedTouchThisFrame = false;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (!touch.press.isPressed)
                continue;

            activeTouches++;
            if (touch.press.wasPressedThisFrame)
                startedTouchThisFrame = true;

            if (activeTouches >= 2 && startedTouchThisFrame)
                return true;
        }

        return false;
    }

    // Toggle pause state
    private void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    // Pause gameplay and show action buttons
    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        thrustSource.Stop();
        if (rocketFlame != null) rocketFlame.SetActive(false);

        gameUI?.HideNameEntry();
        gameUI?.SetHudVisible(false);
        gameUI?.ShowPauseButtons(true);
    }

    // Resume gameplay and hide pause buttons
    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        gameUI?.ShowPauseButtons(false);
        gameUI?.SetHudVisible(true);
    }

    // Resume gameplay from pause menu button
    private void ResumeGameFromButton()
    {
        if (!isPaused) return;
        ResumeGame();
    }
}
