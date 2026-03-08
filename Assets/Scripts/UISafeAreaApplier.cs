using UnityEngine;
using UnityEngine.UIElements;

public sealed class UISafeAreaApplier
{
    private readonly VisualElement target;
    private Rect lastSafeArea;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    public UISafeAreaApplier(VisualElement root, string targetName = null)
    {
        if (root == null)
            return;

        target = string.IsNullOrWhiteSpace(targetName) ? root : root.Q<VisualElement>(targetName) ?? root;
        Apply(force: true);
    }

    public void ApplyIfChanged()
    {
        Apply(force: false);
    }

    private void Apply(bool force)
    {
        if (target == null)
            return;

        Rect safeArea = Screen.safeArea;
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        ScreenOrientation orientation = Screen.orientation;

        bool changed = safeArea != lastSafeArea
            || screenWidth != lastScreenWidth
            || screenHeight != lastScreenHeight
            || orientation != lastOrientation;

        if (!force && !changed)
            return;

        lastSafeArea = safeArea;
        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;
        lastOrientation = orientation;

        float leftInset = Mathf.Max(0f, safeArea.xMin);
        float rightInset = Mathf.Max(0f, screenWidth - safeArea.xMax);
        float topInset = Mathf.Max(0f, screenHeight - safeArea.yMax);
        float bottomInset = Mathf.Max(0f, safeArea.yMin);

        target.style.paddingLeft = leftInset;
        target.style.paddingRight = rightInset;
        target.style.paddingTop = topInset;
        target.style.paddingBottom = bottomInset;
    }
}
