using System.Buffers.Binary;
using System.Text.Json;

namespace LinuxEdgeInspection.Ipc.Serialization;

/// <summary>
/// 4 byteのBig Endian長とJSON本文でメッセージを送受信します。
/// </summary>
public static class LengthPrefixedJsonMessageFraming
{
    public const int DefaultMaximumMessageLength = 1024 * 1024;

    private const int HeaderLength = sizeof(int);

    public static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static async ValueTask WriteAsync<T>(
        Stream stream,
        T message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            message,
            SerializerOptions);

        if (payload.Length > DefaultMaximumMessageLength)
        {
            throw new InvalidDataException(
                $"Message length {payload.Length} exceeds the maximum " +
                $"of {DefaultMaximumMessageLength} bytes.");
        }

        var header = new byte[HeaderLength];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async ValueTask<T> ReadAsync<T>(
        Stream stream,
        CancellationToken cancellationToken = default,
        int maximumMessageLength = DefaultMaximumMessageLength)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (maximumMessageLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessageLength));
        }

        var header = new byte[HeaderLength];
        await stream.ReadExactlyAsync(header, cancellationToken);

        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
        if (payloadLength <= 0 || payloadLength > maximumMessageLength)
        {
            throw new InvalidDataException(
                $"Invalid message length: {payloadLength}.");
        }

        var payload = new byte[payloadLength];
        await stream.ReadExactlyAsync(payload, cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<T>(
                payload,
                SerializerOptions)
                ?? throw new InvalidDataException(
                    "JSON payload was null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "JSON payload was invalid.",
                exception);
        }
    }
}
