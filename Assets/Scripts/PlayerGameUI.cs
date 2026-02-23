using System;
using UnityEngine.UIElements;

public class PlayerGameUI
{
    // HUD labels
    private readonly Label scoreText;
    private readonly Label highScoreText;

    // Game over buttons
    private readonly Button restartButton;
    private readonly Button mainMenuButton;

    // Name entry widgets
    private readonly VisualElement namePanel;
    private readonly TextField nameInput;
    private readonly Button saveScoreButton;
    private readonly Button skipSaveButton;

    public PlayerGameUI(VisualElement root)
    {
        scoreText = root.Q<Label>("ScoreText");
        highScoreText = root.Q<Label>("HighScoreText");
        restartButton = root.Q<Button>("RestartButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        namePanel = root.Q<VisualElement>("NamePanel");
        nameInput = root.Q<TextField>("NameInput");
        saveScoreButton = root.Q<Button>("SaveScoreButton");
        skipSaveButton = root.Q<Button>("SkipSaveButton");
    }

    // Initialize default UI state
    public void Initialize(int bestScore)
    {
        ShowGameOverButtons(false);
        HideNameEntry();
        SetHudVisible(true);
        SetHighScore(bestScore);
    }

    // Bind gameplay UI actions
    public void BindEvents(Action onRestart, Action onMainMenu, Action onSave, Action onSkip)
    {
        if (restartButton != null) restartButton.clicked += onRestart;
        if (mainMenuButton != null) mainMenuButton.clicked += onMainMenu;
        if (saveScoreButton != null) saveScoreButton.clicked += onSave;
        if (skipSaveButton != null) skipSaveButton.clicked += onSkip;
    }

    // Unbind gameplay UI actions
    public void UnbindEvents(Action onRestart, Action onMainMenu, Action onSave, Action onSkip)
    {
        if (restartButton != null) restartButton.clicked -= onRestart;
        if (mainMenuButton != null) mainMenuButton.clicked -= onMainMenu;
        if (saveScoreButton != null) saveScoreButton.clicked -= onSave;
        if (skipSaveButton != null) skipSaveButton.clicked -= onSkip;
    }

    // Update current score label
    public void SetScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    // Update high score label
    public void SetHighScore(int score)
    {
        if (highScoreText != null) highScoreText.text = $"High Score: {score}";
    }

    // Show or hide HUD labels
    public void SetHudVisible(bool visible)
    {
        var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (scoreText != null) scoreText.style.display = display;
        if (highScoreText != null) highScoreText.style.display = display;
    }

    // Show or hide restart and menu buttons
    public void ShowGameOverButtons(bool visible)
    {
        var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (restartButton != null) restartButton.style.display = display;
        if (mainMenuButton != null) mainMenuButton.style.display = display;
    }

    // Show name entry panel and focus input
    public void ShowNameEntry(string defaultName)
    {
        if (namePanel != null) namePanel.style.display = DisplayStyle.Flex;

        if (nameInput != null)
        {
            nameInput.value = string.IsNullOrWhiteSpace(defaultName) ? "AAA" : defaultName;
            nameInput.Focus();
            nameInput.SelectAll();
        }
    }

    // Hide name entry panel
    public void HideNameEntry()
    {
        if (namePanel != null) namePanel.style.display = DisplayStyle.None;
    }

    // Read player name from input
    public string GetPlayerNameOrDefault(string fallback)
    {
        string value = nameInput != null ? nameInput.value : string.Empty;
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
