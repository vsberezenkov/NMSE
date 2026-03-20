using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NMSE.Extractor.Data;

public static class JsonWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IndentCharacter = '\t',
        IndentSize = 1,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new DoubleStyleConverter() },
    };

    private static readonly JsonSerializerOptions SpaceIndentOptions = new()
    {
        WriteIndented = true,
        IndentCharacter = ' ',
        IndentSize = 2,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new DoubleStyleConverter() },
    };

    public static double SaveJson(List<Dictionary<string, object?>> data, string outputDir, string filename, bool useSpaceIndent = false)
    {
        Directory.CreateDirectory(outputDir);
        string path = Path.Combine(outputDir, filename);
        var opts = useSpaceIndent ? SpaceIndentOptions : Options;
        string json = JsonSerializer.Serialize(data, opts);
        File.WriteAllText(path, json);
        return new FileInfo(path).Length / 1024.0;
    }

    public static void SaveJsonRaw(object data, string outputDir, string filename)
    {
        Directory.CreateDirectory(outputDir);
        string path = Path.Combine(outputDir, filename);
        string json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Serializes doubles:
    /// - Zero values written as "0.0" (not "0")
    /// - Scientific notation uses lowercase 'e' (not 'E')
    /// </summary>
    internal sealed class DoubleStyleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            string formatted = value.ToString("G", CultureInfo.InvariantCulture);
            // Ensure float values always have a decimal point (e.g., "0" -> "0.0")
            if (!formatted.Contains('.') && !formatted.Contains('E') && !formatted.Contains('e'))
                formatted += ".0";
            // Use lowercase 'e' for scientific notation
            formatted = formatted.Replace("E", "e");
            writer.WriteRawValue(formatted);
        }
    }
}
