namespace NMSE.Models;

/// <summary>
/// Listener interface for observing property changes on JSON objects.
/// </summary>
public interface IPropertyChangeListener
{
    /// <summary>
    /// Called when a property value changes on the observed object.
    /// </summary>
    /// <param name="path">The dotted path of the changed property.</param>
    /// <param name="oldValue">The previous value before the change.</param>
    /// <param name="newValue">The new value after the change.</param>
    void PropertyChanged(string path, object? oldValue, object? newValue);
}
