namespace NMSE.IO;

/// <summary>
/// Buffered LZ4 compression stream.
/// Accumulates all data and compresses it in a single block on close/dispose.
/// </summary>
public class Lz4BufferedCompressorStream : Stream
{
    private const int BlockSize = 65536;
    private readonly Stream _inner;
    private byte[] _buffer;
    private int _bufferPos;
    private int _compressedSize;

    /// <summary>Gets the total number of uncompressed bytes written so far.</summary>
    public int UncompressedSize => _bufferPos;
    /// <summary>Gets the total number of compressed bytes produced.</summary>
    public int CompressedSize => _compressedSize;

    /// <summary>
    /// Initializes a new instance wrapping the specified output stream.
    /// </summary>
    /// <param name="innerStream">The stream to write compressed data to on dispose.</param>
    public Lz4BufferedCompressorStream(Stream innerStream)
    {
        _inner = innerStream;
        _buffer = new byte[BlockSize];
        _bufferPos = 0;
        _compressedSize = 0;
    }

    private void EnsureCapacity(int additional)
    {
        if (_bufferPos + additional <= _buffer.Length) return;
        int needed = _buffer.Length + additional;
        int blocks = (needed + BlockSize - 1) / BlockSize;
        var newBuffer = new byte[blocks * BlockSize];
        Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _bufferPos);
        _buffer = newBuffer;
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_bufferPos++] = value;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureCapacity(count);
        Buffer.BlockCopy(buffer, offset, _buffer, _bufferPos, count);
        _bufferPos += count;
    }

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (_bufferPos > 0)
                {
                    int maxLen = Lz4Compressor.MaxCompressedLength(_bufferPos);
                    byte[] compressed = new byte[maxLen];
                    _compressedSize = Lz4Compressor.Compress(_buffer, 0, _bufferPos, compressed, 0, maxLen);
                    _inner.Write(compressed, 0, _compressedSize);
                }
            }
            finally
            {
                _inner.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override bool CanRead => false;
    /// <inheritdoc />
    public override bool CanSeek => false;
    /// <inheritdoc />
    public override bool CanWrite => true;
    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();
}
