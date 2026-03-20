namespace NMSE.UI.Util;

/// <summary>
/// Cross-platform painting suspension for WinForms controls.
/// Hides the control before a batch update and shows it afterwards,
/// preventing all intermediate redraws. The single re-show triggers
/// one flicker-free repaint of the control and all its children.
/// Call <see cref="Suspend"/> before a batch update and <see cref="Resume"/>
/// afterwards.
/// </summary>
internal static class RedrawHelper
{
    /// <summary>
    /// Suspends painting on the control by hiding it.
    /// No paint messages will be processed until <see cref="Resume"/> is called.
    /// </summary>
    public static void Suspend(Control control)
    {
        control.Visible = false;
    }

    /// <summary>
    /// Resumes painting on the control by making it visible again,
    /// triggering a full synchronous repaint of the control and all its children.
    /// </summary>
    public static void Resume(Control control)
    {
        control.Visible = true;
        control.Invalidate(true);
        control.Update();
    }
}
