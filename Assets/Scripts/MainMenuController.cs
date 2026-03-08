using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void TryCloseBrowserWindow();
#endif

    private enum MenuView
    {
        Main,
        HighScores,
    }

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("High Scores Scroll")]
    [SerializeField, Min(0f)] private float gamepadScrollSpeed = 700f;
    [SerializeField, Range(0.05f, 1f)] private float gamepadScrollDeadzone = 0.2f;

    // Menu containers
    private VisualElement menuPanel;
    private VisualElement highScoresPanel;
    private Label gameTitle;

    // Menu buttons
    private Button playButton;
    private Button highScoresButton;
    private Button quitButton;
    private Button backButton;
    private Button creditsButton;

    // High scores list
    private ScrollView scoresScroll;
    private UIMenuGamepadNavigator gamepadNavigator;
    private MenuView currentView;
    private UISafeAreaApplier safeAreaApplier;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;
        safeAreaApplier = new UISafeAreaApplier(root, "SafeArea");

        menuPanel = root.Q<VisualElement>("MenuPanel");
        highScoresPanel = root.Q<VisualElement>("HighScoresPanel");
        gameTitle = root.Q<Label>("GameTitle");

        playButton = root.Q<Button>("PlayButton");
        highScoresButton = root.Q<Button>("HighScoresButton");
        quitButton = root.Q<Button>("QuitButton");
        backButton = root.Q<Button>("BackButton");
        creditsButton = root.Q<Button>("CreditsButton");

        scoresScroll = root.Q<ScrollView>("ScoresScroll");
        gamepadNavigator = new UIMenuGamepadNavigator();

        // Apply background image
        ApplyBackground(root);

        if (playButton != null) playButton.clicked += OnPlay;
        if (highScoresButton != null) highScoresButton.clicked += ShowHighScores;
        if (quitButton != null) quitButton.clicked += OnQuit;
        if (backButton != null) backButton.clicked += ShowMenu;
        if (creditsButton != null) creditsButton.clicked += OpenCredits;

        ShowMenu();
    }

    private void Update()
    {
        safeAreaApplier?.ApplyIfChanged();

        if (gamepadNavigator == null)
            return;

        gamepadNavigator.TickNavigation();
        TickHighScoresScroll();

        if (UIMenuGamepadNavigator.WasCancelPressedThisFrame() && currentView == MenuView.HighScores)
        {
            ShowMenu();
            return;
        }

        if (!UIMenuGamepadNavigator.WasSubmitPressedThisFrame())
            return;

        var selected = gamepadNavigator.CurrentButton;
        if (selected == null)
            return;

        if (selected == playButton) OnPlay();
        else if (selected == highScoresButton) ShowHighScores();
        else if (selected == quitButton) OnQuit();
        else if (selected == backButton) ShowMenu();
        else if (selected == creditsButton) OpenCredits();
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.clicked -= OnPlay;
        if (highScoresButton != null) highScoresButton.clicked -= ShowHighScores;
        if (quitButton != null) quitButton.clicked -= OnQuit;
        if (backButton != null) backButton.clicked -= ShowMenu;
        if (creditsButton != null) creditsButton.clicked -= OpenCredits;

        gamepadNavigator?.Clear();
    }

    private void ApplyBackground(VisualElement root)
    {
        var bg = root.Q<VisualElement>("Background");
        if (bg == null || backgroundSprite == null) return;

        bg.style.backgroundImage = new StyleBackground(backgroundSprite);

        // Keep background centered and covering the full screen
        bg.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        bg.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        bg.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
    }

    private void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnQuit()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TryCloseBrowserWindow();
#else
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
#endif
    }

    private void OpenCredits()
    {
        Application.OpenURL("https://nabil-labrazi.fr");
    }

    private void ShowMenu()
    {
        if (gameTitle != null) gameTitle.text = "SPRITE FLIGHT";
        if (menuPanel != null) menuPanel.style.display = DisplayStyle.Flex;
        if (highScoresPanel != null) highScoresPanel.style.display = DisplayStyle.None;
        if (creditsButton != null) creditsButton.style.display = DisplayStyle.Flex;

        currentView = MenuView.Main;
        RefreshNavigation(resetSelection: true);
    }

    private void ShowHighScores()
    {
        if (gameTitle != null) gameTitle.text = "SCOREBOARD";
        if (menuPanel != null) menuPanel.style.display = DisplayStyle.None;
        if (highScoresPanel != null) highScoresPanel.style.display = DisplayStyle.Flex;
        if (creditsButton != null) creditsButton.style.display = DisplayStyle.None;

        RenderTop10();

        currentView = MenuView.HighScores;
        RefreshNavigation(resetSelection: true);
    }

    // Refresh high scores content
    private void RenderTop10()
    {
        if (scoresScroll == null) return;

        scoresScroll.contentContainer.Clear();

        var top10 = ScoreboardManager.LoadTop10();
        if (top10.Count == 0)
        {
            scoresScroll.Add(new Label("No scores yet"));
            scoresScroll.scrollOffset = Vector2.zero;
            return;
        }

        for (int i = 0; i < top10.Count; i++)
        {
            var e = top10[i];
            scoresScroll.Add(new Label($"{i + 1}. {e.playerName} - {e.score}"));
        }

        // Start at the top of the list
        scoresScroll.scrollOffset = Vector2.zero;
    }

    private void RefreshNavigation(bool resetSelection)
    {
        if (gamepadNavigator == null)
            return;

        if (currentView == MenuView.HighScores)
        {
            gamepadNavigator.SetButtons(new[] { backButton }, resetSelection);
            return;
        }

        gamepadNavigator.SetButtons(new[] { playButton, highScoresButton, quitButton, creditsButton }, resetSelection);
    }

    private void TickHighScoresScroll()
    {
        if (currentView != MenuView.HighScores || scoresScroll == null || Gamepad.current == null)
            return;

        float stickY = Gamepad.current.rightStick.ReadValue().y;
        if (Mathf.Abs(stickY) < gamepadScrollDeadzone)
            return;

        float deltaY = -stickY * gamepadScrollSpeed * Time.unscaledDeltaTime;
        float maxY = GetScoresScrollMaxY();

        Vector2 nextOffset = scoresScroll.scrollOffset;
        float rawY = nextOffset.y + deltaY;
        nextOffset.y = float.IsPositiveInfinity(maxY)
            ? Mathf.Max(0f, rawY)
            : Mathf.Clamp(rawY, 0f, maxY);

        scoresScroll.scrollOffset = nextOffset;
    }

    private float GetScoresScrollMaxY()
    {
        if (scoresScroll?.verticalScroller == null)
            return float.PositiveInfinity;

        return Mathf.Max(0f, scoresScroll.verticalScroller.highValue);
    }
}
