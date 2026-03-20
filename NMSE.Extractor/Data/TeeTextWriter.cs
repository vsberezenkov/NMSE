using System.Text;

namespace NMSE.Extractor.Data;

/// <summary>
/// A TextWriter that forwards all writes to both the original console writer and a log writer.
/// Used to mirror all console output to a log file.
/// (The log writer is NOT owned by this class. The caller is responsible for disposing it.)
/// </summary>
public sealed class TeeTextWriter : TextWriter
{
    private readonly TextWriter _console;
    private readonly TextWriter _log;

    public TeeTextWriter(TextWriter console, TextWriter log)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public override Encoding Encoding => _console.Encoding;

    public override void Write(char value)
    {
        _console.Write(value);
        _log.Write(value);
    }

    public override void Write(string? value)
    {
        _console.Write(value);
        _log.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _console.WriteLine(value);
        _log.WriteLine(value);
    }

    public override void WriteLine()
    {
        _console.WriteLine();
        _log.WriteLine();
    }

    public override void Flush()
    {
        _console.Flush();
        _log.Flush();
    }
}
