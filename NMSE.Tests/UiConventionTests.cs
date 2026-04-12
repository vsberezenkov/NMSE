using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace NMSE.Tests;

/// <summary>
/// Source-level convention tests that scan the UI code to enforce consistency
/// rules that are easy to regress on. These verify code patterns rather than
/// runtime behaviour.
/// </summary>
public class UiConventionTests
{
    // TODO: This class needs to be built upon over time as new conventions are added, as well as old ones being captured - but bigger fish to fry right now.
    // In the future, consider using Roslyn.

    private readonly ITestOutputHelper _output;
    private const string UiDir = "../../../../../UI";

    public UiConventionTests(ITestOutputHelper output) { _output = output; }

    /// <summary>
    /// Every MessageBox.Show call in the UI layer should pass an owner window
    /// (typically 'this') as the first parameter so that the dialog centres on
    /// the parent form instead of the screen. The only exceptions are calls
    /// inside static methods where 'this' is unavailable.
    /// </summary>
    [Fact]
    public void AllMessageBoxShowCalls_ShouldUseOwnerParameter()
    {
        if (!Directory.Exists(UiDir))
        {
            _output.WriteLine("UI directory not found, skipping.");
            return;
        }

        var csFiles = Directory.GetFiles(UiDir, "*.cs", SearchOption.AllDirectories);
        var violations = new System.Collections.Generic.List<string>();

        // Matches MessageBox.Show( NOT followed by this, or FindForm
        var pattern = new Regex(@"MessageBox\.Show\((?!this[,\s])(?!FindForm)", RegexOptions.Compiled);

        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    // Allow static methods where 'this' is not available
                    bool inStatic = IsInsideStaticMethod(lines, i);
                    if (!inStatic)
                    {
                        string relative = Path.GetRelativePath(UiDir, file);
                        violations.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.Empty(violations);
    }

    /// <summary>
    /// CompanionPanel.cs must declare an ExosuitCargoModified event so that
    /// MainForm can refresh the exosuit inventory grid after an egg is placed.
    /// </summary>
    [Fact]
    public void CompanionPanel_DeclaresExosuitCargoModifiedEvent()
    {
        string panelPath = Path.Combine(UiDir, "Panels", "CompanionPanel.cs");
        if (!File.Exists(panelPath))
        {
            _output.WriteLine("CompanionPanel.cs not found, skipping.");
            return;
        }

        string content = File.ReadAllText(panelPath);
        Assert.Contains("public event EventHandler? ExosuitCargoModified", content);
    }

    /// <summary>
    /// MainForm.cs must subscribe to CompanionPanel.ExosuitCargoModified so
    /// the exosuit inventory grid refreshes when an egg is placed in cargo.
    /// </summary>
    [Fact]
    public void MainForm_SubscribesToExosuitCargoModified()
    {
        string mainFormPath = Path.Combine(UiDir, "MainForm.cs");
        if (!File.Exists(mainFormPath))
        {
            _output.WriteLine("MainForm.cs not found, skipping.");
            return;
        }

        string content = File.ReadAllText(mainFormPath);
        Assert.Contains("ExosuitCargoModified", content);
        Assert.Contains("_exosuitPanel.LoadData", content);
    }

    /// <summary>
    /// Heuristic check: scans backwards from the given line looking for a
    /// 'static' keyword in a method signature before hitting a closing brace
    /// at column 0-4 (indicating a prior method boundary).
    /// </summary>
    private static bool IsInsideStaticMethod(string[] lines, int lineIndex)
    {
        for (int i = lineIndex; i >= 0; i--)
        {
            string trimmed = lines[i].TrimStart();
            // Look for method-like signatures
            if (trimmed.Contains("static ") && (trimmed.Contains("void ") || trimmed.Contains("bool ")
                || trimmed.Contains("string ") || trimmed.Contains("int ") || trimmed.Contains("Task ")))
                return true;
            // Stop scanning at class/struct boundary
            if (trimmed.StartsWith("public class ") || trimmed.StartsWith("internal class ")
                || trimmed.StartsWith("private class ") || trimmed.StartsWith("public partial class "))
                return false;
        }
        return false;
    }
}
