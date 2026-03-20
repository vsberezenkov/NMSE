namespace NMSE.IO;

/// <summary>
/// Provides low-level binary I/O helpers for reading and writing little-endian integers, byte arrays, and Base64 encoding.
/// </summary>
public static class BinaryIO
{
    private const string Base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    /// <summary>
    /// Reads a 32-bit integer in little-endian byte order from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The 32-bit integer value.</returns>
    public static int ReadInt32LE(Stream stream)
    {
        Span<byte> buf = stackalloc byte[4];
        ReadFully(stream, buf);
        return buf[0] | (buf[1] << 8) | (buf[2] << 16) | (buf[3] << 24);
    }

    /// <summary>
    /// Writes a 32-bit integer in little-endian byte order to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The 32-bit integer value to write.</param>
    public static void WriteInt32LE(Stream stream, int value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    /// <summary>
    /// Reads a 64-bit integer in little-endian byte order from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The 64-bit integer value.</returns>
    public static long ReadInt64LE(Stream stream)
    {
        Span<byte> buf = stackalloc byte[8];
        ReadFully(stream, buf);
        return (long)buf[0] | ((long)buf[1] << 8) | ((long)buf[2] << 16) | ((long)buf[3] << 24) |
               ((long)buf[4] << 32) | ((long)buf[5] << 40) | ((long)buf[6] << 48) | ((long)buf[7] << 56);
    }

    /// <summary>
    /// Writes a 64-bit integer in little-endian byte order to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="value">The 64-bit integer value to write.</param>
    public static void WriteInt64LE(Stream stream, long value)
    {
        for (int i = 0; i < 8; i++)
            stream.WriteByte((byte)((value >> (i * 8)) & 0xFF));
    }

    /// <summary>
    /// Reads all remaining bytes from a stream into a byte array.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A byte array containing all bytes read from the stream.</returns>
    public static byte[] ReadAllBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Reads all bytes from a file at the specified path.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <returns>A byte array containing the file contents.</returns>
    public static byte[] ReadFileBytes(string path)
    {
        using var fs = File.OpenRead(path);
        return ReadAllBytes(fs);
    }

    /// <summary>
    /// Reads exactly <paramref name="buffer"/>.Length bytes from the stream, throwing on short reads.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The span to fill with data.</param>
    public static void ReadFully(Stream stream, Span<byte> buffer)
    {
        int offset = 0;
        int remaining = buffer.Length;
        while (remaining > 0)
        {
            int read = stream.Read(buffer.Slice(offset, remaining));
            if (read <= 0) throw new IOException("Short read");
            offset += read;
            remaining -= read;
        }
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes from the stream into the buffer, throwing on short reads.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The destination byte array.</param>
    /// <param name="offset">The offset in the buffer to start writing.</param>
    /// <param name="count">The number of bytes to read.</param>
    public static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int read = stream.Read(buffer, offset, count);
            if (read <= 0) throw new IOException("Short read");
            offset += read;
            count -= read;
        }
    }

    /// <summary>
    /// Encodes a byte array to a Base64 string.
    /// </summary>
    /// <param name="data">The byte array to encode.</param>
    /// <returns>The Base64-encoded string.</returns>
    public static string Base64Encode(byte[] data)
    {
        // Use native .NET Base64
        return Convert.ToBase64String(data);
    }

    /// <summary>
    /// Decodes a Base64 string to a byte array.
    /// </summary>
    /// <param name="encoded">The Base64-encoded string to decode.</param>
    /// <returns>The decoded byte array.</returns>
    public static byte[] Base64Decode(string encoded)
    {
        // Use native .NET Base64
        return Convert.FromBase64String(encoded);
    }
}
