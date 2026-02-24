using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

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

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;

        menuPanel = root.Q<VisualElement>("MenuPanel");
        highScoresPanel = root.Q<VisualElement>("HighScoresPanel");
        gameTitle = root.Q<Label>("GameTitle");

        playButton = root.Q<Button>("PlayButton");
        highScoresButton = root.Q<Button>("HighScoresButton");
        quitButton = root.Q<Button>("QuitButton");
        backButton = root.Q<Button>("BackButton");
        creditsButton = root.Q<Button>("CreditsButton");

        scoresScroll = root.Q<ScrollView>("ScoresScroll");

        // Apply background image
        ApplyBackground(root);

        if (playButton != null) playButton.clicked += OnPlay;
        if (highScoresButton != null) highScoresButton.clicked += ShowHighScores;
        if (quitButton != null) quitButton.clicked += OnQuit;
        if (backButton != null) backButton.clicked += ShowMenu;
        if (creditsButton != null) creditsButton.clicked += OpenCredits;

        ShowMenu();
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.clicked -= OnPlay;
        if (highScoresButton != null) highScoresButton.clicked -= ShowHighScores;
        if (quitButton != null) quitButton.clicked -= OnQuit;
        if (backButton != null) backButton.clicked -= ShowMenu;
        if (creditsButton != null) creditsButton.clicked -= OpenCredits;
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
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
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
    }

    private void ShowHighScores()
    {
        if (gameTitle != null) gameTitle.text = "SCOREBOARD";
        if (menuPanel != null) menuPanel.style.display = DisplayStyle.None;
        if (highScoresPanel != null) highScoresPanel.style.display = DisplayStyle.Flex;

        RenderTop10();
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
}
