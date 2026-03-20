using NMSE.IO;

namespace NMSE.Tests;

/// <summary>
/// Tests for IO layer classes: BinaryIO and Lz4Compressor.
/// </summary>
public class IOLayerTests
{
    // --- BinaryIO: Int32 LE round-trip -------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0x12345678)]
    public void BinaryIO_ReadWriteInt32LE_RoundTrip(int value)
    {
        using var ms = new MemoryStream();
        BinaryIO.WriteInt32LE(ms, value);
        ms.Position = 0;
        int result = BinaryIO.ReadInt32LE(ms);
        Assert.Equal(value, result);
    }

    [Fact]
    public void BinaryIO_WriteInt32LE_ProducesLittleEndianBytes()
    {
        using var ms = new MemoryStream();
        BinaryIO.WriteInt32LE(ms, 0x04030201);
        byte[] bytes = ms.ToArray();
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x01, bytes[0]);
        Assert.Equal(0x02, bytes[1]);
        Assert.Equal(0x03, bytes[2]);
        Assert.Equal(0x04, bytes[3]);
    }

    // --- BinaryIO: Int64 LE round-trip -------------------------------

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void BinaryIO_ReadWriteInt64LE_RoundTrip(long value)
    {
        using var ms = new MemoryStream();
        BinaryIO.WriteInt64LE(ms, value);
        ms.Position = 0;
        long result = BinaryIO.ReadInt64LE(ms);
        Assert.Equal(value, result);
    }

    // --- BinaryIO: Base64 round-trip ---------------------------------

    [Fact]
    public void BinaryIO_Base64EncodeDecode_RoundTrip()
    {
        byte[] data = { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD, 0x80, 0x7F };
        string encoded = BinaryIO.Base64Encode(data);
        byte[] decoded = BinaryIO.Base64Decode(encoded);
        Assert.Equal(data, decoded);
    }

    [Fact]
    public void BinaryIO_Base64Encode_EmptyArray_ReturnsEmptyString()
    {
        Assert.Equal("", BinaryIO.Base64Encode(Array.Empty<byte>()));
    }

    [Fact]
    public void BinaryIO_Base64Decode_EmptyString_ReturnsEmptyArray()
    {
        Assert.Empty(BinaryIO.Base64Decode(""));
    }

    [Fact]
    public void BinaryIO_Base64Encode_ProducesValidBase64()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
        string encoded = BinaryIO.Base64Encode(data);
        Assert.Equal("SGVsbG8sIFdvcmxkIQ==", encoded);
    }

    // --- BinaryIO: ReadAllBytes --------------------------------------

    [Fact]
    public void BinaryIO_ReadAllBytes_ReadsEntireStream()
    {
        byte[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var ms = new MemoryStream(expected);
        byte[] result = BinaryIO.ReadAllBytes(ms);
        Assert.Equal(expected, result);
    }

    // --- BinaryIO: ReadFully -----------------------------------------

    [Fact]
    public void BinaryIO_ReadFully_ThrowsOnShortRead()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2 });
        byte[] buf = new byte[4];
        Assert.Throws<IOException>(() => BinaryIO.ReadFully(ms, buf, 0, 4));
    }

    // --- Lz4Compressor: round-trip -----------------------------------

    [Fact]
    public void Lz4Compressor_CompressDecompress_RoundTrip_SimpleData()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes(
            "Hello World! Hello World! Hello World! Hello World! " +
            "This is a test of LZ4 compression and decompression.");
        
        byte[] compressed = new byte[Lz4Compressor.MaxCompressedLength(original.Length)];
        int compressedLen = Lz4Compressor.Compress(original, 0, original.Length,
            compressed, 0, compressed.Length);

        Assert.True(compressedLen > 0);

        byte[] decompressed = new byte[original.Length];
        int decompressedLen = Lz4Compressor.Decompress(compressed, 0, compressedLen,
            decompressed, 0, decompressed.Length);

        Assert.Equal(original.Length, decompressedLen);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Lz4Compressor_CompressDecompress_RoundTrip_RepetitiveData()
    {
        // Highly repetitive data should compress well
        byte[] original = new byte[4096];
        for (int i = 0; i < original.Length; i++)
            original[i] = (byte)(i % 16);

        byte[] compressed = new byte[Lz4Compressor.MaxCompressedLength(original.Length)];
        int compressedLen = Lz4Compressor.Compress(original, 0, original.Length,
            compressed, 0, compressed.Length);

        Assert.True(compressedLen > 0);
        Assert.True(compressedLen < original.Length, "Repetitive data should compress smaller");

        byte[] decompressed = new byte[original.Length];
        int decompressedLen = Lz4Compressor.Decompress(compressed, 0, compressedLen,
            decompressed, 0, decompressed.Length);

        Assert.Equal(original.Length, decompressedLen);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Lz4Compressor_Compress_EmptyInput_ReturnsZero()
    {
        byte[] compressed = new byte[64];
        int len = Lz4Compressor.Compress(Array.Empty<byte>(), 0, 0, compressed, 0, compressed.Length);
        Assert.Equal(0, len);
    }

    [Fact]
    public void Lz4Compressor_MaxCompressedLength_ReturnsPositive()
    {
        Assert.True(Lz4Compressor.MaxCompressedLength(100) > 100);
        Assert.True(Lz4Compressor.MaxCompressedLength(0) >= 0);
    }

    [Fact]
    public void Lz4Compressor_MaxCompressedLength_NegativeInput_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Lz4Compressor.MaxCompressedLength(-1));
    }

    [Fact]
    public void Lz4Compressor_CompressDecompress_RoundTrip_RandomData()
    {
        var rng = new Random(42);
        byte[] original = new byte[8192];
        rng.NextBytes(original);

        byte[] compressed = new byte[Lz4Compressor.MaxCompressedLength(original.Length)];
        int compressedLen = Lz4Compressor.Compress(original, 0, original.Length,
            compressed, 0, compressed.Length);

        Assert.True(compressedLen > 0);

        byte[] decompressed = new byte[original.Length];
        int decompressedLen = Lz4Compressor.Decompress(compressed, 0, compressedLen,
            decompressed, 0, decompressed.Length);

        Assert.Equal(original.Length, decompressedLen);
        Assert.Equal(original, decompressed);
    }
}
