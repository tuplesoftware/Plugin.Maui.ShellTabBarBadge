using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using UIKit;

namespace Plugin.Maui.ShellTabBarBadge;

/// <summary>
/// Custom Shell renderer that injects badge support into the iOS tab bar.
/// </summary>
public class TabBarBadgeRenderer : ShellRenderer
{
    /// <summary>
    /// Creates the custom tab bar appearance tracker that adds badge rendering support.
    /// </summary>
    /// <returns>
    /// A <see cref="BadgeShellTabBarAppearanceTracker"/> instance for managing tab bar badges.
    /// </returns>
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
        => new BadgeShellTabBarAppearanceTracker();
}

// Tracks current UITabBarController
internal class BadgeShellTabBarAppearanceTracker : ShellTabBarAppearanceTracker
{
    internal static UITabBarController? s_controller { get; private set; }

    public override void UpdateLayout(UITabBarController controller)
    {
        base.UpdateLayout(controller);

        s_controller = controller; // keep latest controller
    }
}

internal static class SharedExtensions
{
    static UIView? GetTabButton(this UITabBarController controller, int tabIndex)
    {
        var tabBar = controller?.TabBar;
        if (tabBar == null) return null;

        var buttons = tabBar.Subviews
            .Where(v => v is UIControl || v.GetType().Name.Contains("UITabBarButton"))
            .OrderBy(v => v.Frame.X)
            .ToArray();

        return (buttons.Length > tabIndex) ? buttons[tabIndex] : null;
    }

    internal static void SetBadge(
        this UITabBarController controller,
        int tabIndex,
        bool isDot,
        string? text,
        UIColor? bg,
        UIColor? fg,
        int anchorX,
        int anchorY,
        HorizontalAlignment horizontal,
        VerticalAlignment vertical,
        double fontSize)
    {
        var button = controller.GetTabButton(tabIndex);
        if (button == null) return;

        var tag = 90000 + tabIndex; // unique per tab
        var existing = button.ViewWithTag(tag);
        existing?.RemoveFromSuperview();

        if (!isDot && string.IsNullOrWhiteSpace(text))
            return;

        var imageView = button.Subviews.FirstOrDefault(v => v is UIImageView);
        var labelView = button.Subviews.FirstOrDefault(v => v is UILabel);

        if (isDot)
        {
            // ---------- Tiny 8x8 dot ----------
            var dot = new UIView { Tag = tag };
            dot.BackgroundColor = bg ?? UIColor.Red;
            dot.Layer.CornerRadius = 4;
            dot.TranslatesAutoresizingMaskIntoConstraints = false;

            button.AddSubview(dot);

            NSLayoutConstraint.ActivateConstraints(new[] {
                dot.WidthAnchor.ConstraintEqualTo(8),
                dot.HeightAnchor.ConstraintEqualTo(8),
            });

            ApplyPositionConstraints(button, dot, imageView ?? labelView, anchorX, anchorY, horizontal, vertical);
            return;
        }

        // ---------- Text / Number Badge ----------
        const float minHeight = 16f;
        const float sidePadding = 4f;

        var container = new UIView { Tag = tag };
        container.BackgroundColor = bg ?? UIColor.Red;
        container.TranslatesAutoresizingMaskIntoConstraints = false;
        container.Layer.MasksToBounds = true;
        container.Layer.CornerRadius = minHeight / 2f;

        var label = new UILabel
        {
            TextColor = fg ?? UIColor.White,
            Font = UIFont.SystemFontOfSize((nfloat)fontSize, UIFontWeight.Bold),
            TextAlignment = UITextAlignment.Center,
            Lines = 1,
            LineBreakMode = UILineBreakMode.Clip,
            TranslatesAutoresizingMaskIntoConstraints = false,
            Text = text
        };

        button.AddSubview(container);
        container.AddSubview(label);

        NSLayoutConstraint.ActivateConstraints(new[] {
            label.TopAnchor.ConstraintEqualTo(container.TopAnchor, 1),
            label.BottomAnchor.ConstraintEqualTo(container.BottomAnchor, -1),
            label.LeadingAnchor.ConstraintEqualTo(container.LeadingAnchor, sidePadding),
            label.TrailingAnchor.ConstraintEqualTo(container.TrailingAnchor, -sidePadding),
            container.WidthAnchor.ConstraintGreaterThanOrEqualTo(minHeight),
            container.HeightAnchor.ConstraintGreaterThanOrEqualTo(minHeight),
        });

        ApplyPositionConstraints(button, container, imageView ?? labelView, anchorX, anchorY, horizontal, vertical);
    }

    static void ApplyPositionConstraints(
        UIView button,
        UIView badge,
        UIView? refView,
        int anchorX,
        int anchorY,
        HorizontalAlignment horizontal,
        VerticalAlignment vertical)
    {
        if (refView != null)
        {
            // Horizontal
            if (horizontal == HorizontalAlignment.Right)
                badge.LeftAnchor.ConstraintEqualTo(refView.RightAnchor, anchorX).Active = true;
            else if (horizontal == HorizontalAlignment.Center)
                badge.CenterXAnchor.ConstraintEqualTo(refView.CenterXAnchor, anchorX).Active = true;
            else // Left
                badge.RightAnchor.ConstraintEqualTo(refView.LeftAnchor, -anchorX).Active = true;

            // Vertical
            if (vertical == VerticalAlignment.Top)
                badge.TopAnchor.ConstraintEqualTo(refView.TopAnchor, anchorY - 6).Active = true;
            else if (vertical == VerticalAlignment.Center)
                badge.CenterYAnchor.ConstraintEqualTo(refView.CenterYAnchor, anchorY).Active = true;
            else // Bottom
                badge.BottomAnchor.ConstraintEqualTo(refView.BottomAnchor, -anchorY).Active = true;
        }
        else
        {
            // fallback: relative to tab button
            if (horizontal == HorizontalAlignment.Right)
                badge.RightAnchor.ConstraintEqualTo(button.RightAnchor, -anchorX).Active = true;
            else if (horizontal == HorizontalAlignment.Center)
                badge.CenterXAnchor.ConstraintEqualTo(button.CenterXAnchor, anchorX).Active = true;
            else
                badge.LeftAnchor.ConstraintEqualTo(button.LeftAnchor, anchorX).Active = true;

            if (vertical == VerticalAlignment.Top)
                badge.TopAnchor.ConstraintEqualTo(button.TopAnchor, anchorY - 6).Active = true;
            else if (vertical == VerticalAlignment.Center)
                badge.CenterYAnchor.ConstraintEqualTo(button.CenterYAnchor, anchorY).Active = true;
            else
                badge.BottomAnchor.ConstraintEqualTo(button.BottomAnchor, -anchorY).Active = true;
        }
    }
}
