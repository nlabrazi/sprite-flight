using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class PlayerGameUI
{
    private enum NavigationContext
    {
        None,
        Pause,
        GameOver,
        NameEntry,
    }

    // HUD labels
    private readonly Label scoreText;
    private readonly Label highScoreText;

    // Game over buttons
    private readonly Button resumeButton;
    private readonly Button restartButton;
    private readonly Button mainMenuButton;

    // Name entry widgets
    private readonly VisualElement namePanel;
    private readonly TextField nameInput;
    private readonly Button saveScoreButton;
    private readonly Button skipSaveButton;
    private readonly UIMenuGamepadNavigator gamepadNavigator = new();
    private readonly List<Button> navigationBuffer = new();

    private Action onResumeAction;
    private Action onRestartAction;
    private Action onMainMenuAction;
    private Action onSaveAction;
    private Action onSkipAction;
    private NavigationContext activeNavigationContext;

    public PlayerGameUI(VisualElement root)
    {
        scoreText = root.Q<Label>("ScoreText");
        highScoreText = root.Q<Label>("HighScoreText");
        resumeButton = root.Q<Button>("ResumeButton");
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
    public void BindEvents(Action onResume, Action onRestart, Action onMainMenu, Action onSave, Action onSkip)
    {
        onResumeAction = onResume;
        onRestartAction = onRestart;
        onMainMenuAction = onMainMenu;
        onSaveAction = onSave;
        onSkipAction = onSkip;

        if (resumeButton != null) resumeButton.clicked += onResume;
        if (restartButton != null) restartButton.clicked += onRestart;
        if (mainMenuButton != null) mainMenuButton.clicked += onMainMenu;
        if (saveScoreButton != null) saveScoreButton.clicked += onSave;
        if (skipSaveButton != null) skipSaveButton.clicked += onSkip;
    }

    // Unbind gameplay UI actions
    public void UnbindEvents(Action onResume, Action onRestart, Action onMainMenu, Action onSave, Action onSkip)
    {
        if (resumeButton != null) resumeButton.clicked -= onResume;
        if (restartButton != null) restartButton.clicked -= onRestart;
        if (mainMenuButton != null) mainMenuButton.clicked -= onMainMenu;
        if (saveScoreButton != null) saveScoreButton.clicked -= onSave;
        if (skipSaveButton != null) skipSaveButton.clicked -= onSkip;

        onResumeAction = null;
        onRestartAction = null;
        onMainMenuAction = null;
        onSaveAction = null;
        onSkipAction = null;

        activeNavigationContext = NavigationContext.None;
        gamepadNavigator.Clear();
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
        if (resumeButton != null) resumeButton.style.display = DisplayStyle.None;
        if (restartButton != null) restartButton.style.display = display;
        if (mainMenuButton != null) mainMenuButton.style.display = display;
    }

    // Show or hide pause action buttons
    public void ShowPauseButtons(bool visible)
    {
        var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (resumeButton != null) resumeButton.style.display = display;
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

    // Handle gamepad/keyboard menu navigation and validation
    public void TickNavigation(bool enabled)
    {
        if (!enabled)
        {
            activeNavigationContext = NavigationContext.None;
            gamepadNavigator.Clear();
            return;
        }

        NavigationContext context = GetNavigationContext();
        if (context == NavigationContext.None)
        {
            activeNavigationContext = NavigationContext.None;
            gamepadNavigator.Clear();
            return;
        }

        if (context != activeNavigationContext)
        {
            activeNavigationContext = context;
            ConfigureNavigationForContext(context);
        }

        gamepadNavigator.TickNavigation();

        if (UIMenuGamepadNavigator.WasCancelPressedThisFrame())
        {
            HandleCancel(context);
            return;
        }

        if (!UIMenuGamepadNavigator.WasSubmitPressedThisFrame())
            return;

        var selected = gamepadNavigator.CurrentButton;
        if (selected == null)
            return;

        if (selected == resumeButton) onResumeAction?.Invoke();
        else if (selected == restartButton) onRestartAction?.Invoke();
        else if (selected == mainMenuButton) onMainMenuAction?.Invoke();
        else if (selected == saveScoreButton) onSaveAction?.Invoke();
        else if (selected == skipSaveButton) onSkipAction?.Invoke();
    }

    private void HandleCancel(NavigationContext context)
    {
        switch (context)
        {
            case NavigationContext.Pause:
                onResumeAction?.Invoke();
                break;

            case NavigationContext.NameEntry:
                onSkipAction?.Invoke();
                break;

            case NavigationContext.GameOver:
                onMainMenuAction?.Invoke();
                break;
        }
    }

    private NavigationContext GetNavigationContext()
    {
        if (IsVisible(namePanel))
            return NavigationContext.NameEntry;

        if (IsVisible(resumeButton))
            return NavigationContext.Pause;

        if (IsVisible(restartButton))
            return NavigationContext.GameOver;

        return NavigationContext.None;
    }

    private static bool IsVisible(VisualElement element)
    {
        return element != null && element.resolvedStyle.display != DisplayStyle.None;
    }

    private void AddButtonToNavigation(Button button)
    {
        if (button != null && IsVisible(button))
            navigationBuffer.Add(button);
    }

    private void ConfigureNavigationForContext(NavigationContext context)
    {
        navigationBuffer.Clear();

        switch (context)
        {
            case NavigationContext.Pause:
                AddButtonToNavigation(resumeButton);
                AddButtonToNavigation(restartButton);
                AddButtonToNavigation(mainMenuButton);
                break;

            case NavigationContext.GameOver:
                AddButtonToNavigation(restartButton);
                AddButtonToNavigation(mainMenuButton);
                break;

            case NavigationContext.NameEntry:
                AddButtonToNavigation(saveScoreButton);
                AddButtonToNavigation(skipSaveButton);
                break;
        }

        gamepadNavigator.SetButtons(navigationBuffer, resetSelection: true);
    }
}
