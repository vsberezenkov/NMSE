namespace NMSE.UI.Util;

/// <summary>
/// Cross-platform painting suspension for WinForms controls.
/// Uses <see cref="Control.SuspendLayout"/> / <see cref="Control.ResumeLayout(bool)"/>
/// to batch layout changes and prevent intermediate redraws.
/// Unlike toggling <see cref="Control.Visible"/>, layout suspension does not
/// destroy or recreate native window handles in the subtree, which avoids
/// GDI handle exhaustion under heavy control churn.
/// Call <see cref="Suspend"/> before a batch update and <see cref="Resume"/>
/// afterwards.
/// </summary>
internal static class RedrawHelper
{
    /// <summary>
    /// Suspends layout logic on the control.
    /// No layout or paint messages will be processed until <see cref="Resume"/> is called.
    /// </summary>
    public static void Suspend(Control control)
    {
        control.SuspendLayout();
    }

    /// <summary>
    /// Resumes layout on the control and triggers a full synchronous repaint
    /// of the control and all its children.
    /// </summary>
    public static void Resume(Control control)
    {
        control.ResumeLayout(true);
        control.Invalidate(true);
        control.Update();
    }
}
