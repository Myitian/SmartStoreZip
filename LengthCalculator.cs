namespace SmartStoreZip;

sealed class LengthCalculator : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get; set; }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public LengthCalculator Reset()
    {
        Position = 0;
        return this;
    }
    public override void Flush()
    {
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        Position += count;
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Position += buffer.Length;
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Position += count;
        return Task.CompletedTask;
    }
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Position += buffer.Length;
        return ValueTask.CompletedTask;
    }
    public override void WriteByte(byte value)
    {
        Position++;
    }
}