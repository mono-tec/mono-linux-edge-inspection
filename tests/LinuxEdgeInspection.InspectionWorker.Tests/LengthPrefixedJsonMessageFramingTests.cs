using System.Buffers.Binary;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Ipc.Serialization;

namespace LinuxEdgeInspection.InspectionWorker.Tests;

public sealed class LengthPrefixedJsonMessageFramingTests
{
    [Fact]
    public async Task WriteAndReadAsync_RoundTripsMessage()
    {
        var expected = new CaptureRequest(
            "REQ-001", 1, DateTimeOffset.Now);
        await using var stream = new MemoryStream();

        await LengthPrefixedJsonMessageFraming.WriteAsync(stream, expected);
        stream.Position = 0;
        var actual =
            await LengthPrefixedJsonMessageFraming.ReadAsync<CaptureRequest>(
                stream);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1048577)]
    public async Task ReadAsync_WhenLengthIsInvalid_Throws(int length)
    {
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, length);
        await using var stream = new MemoryStream(header);

        await Assert.ThrowsAsync<InvalidDataException>(
            async () =>
                await LengthPrefixedJsonMessageFraming.ReadAsync<CaptureRequest>(
                    stream));
    }

    [Fact]
    public async Task ReadAsync_WhenPayloadIsTruncated_Throws()
    {
        var data = new byte[sizeof(int) + 2];
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0, sizeof(int)), 10);
        await using var stream = new MemoryStream(data);

        await Assert.ThrowsAsync<EndOfStreamException>(
            async () =>
                await LengthPrefixedJsonMessageFraming.ReadAsync<CaptureRequest>(
                    stream));
    }
}
