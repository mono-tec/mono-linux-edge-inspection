using System.Net.Sockets;
using LinuxEdgeInspection.Contracts.Inspection;
using LinuxEdgeInspection.Ipc.Serialization;
using LinuxEdgeInspection.Management.Api.Options;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Management.Api.Services;

public sealed class UnixDomainSocketInspectionWorkerClient
    : IInspectionWorkerClient
{
    private readonly InspectionWorkerClientOptions _options;
    private readonly TimeSpan _timeout;

    public UnixDomainSocketInspectionWorkerClient(
        IOptions<InspectionWorkerClientOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value
            ?? throw new ArgumentNullException(nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.SocketPath);

        if (_options.TimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "TimeoutSeconds must be greater than zero.");
        }

        _timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<InspectionExecutionResult> ExecuteAsync(
        InspectionExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            using var socket = new Socket(
                AddressFamily.Unix,
                SocketType.Stream,
                ProtocolType.Unspecified);
            await socket.ConnectAsync(
                new UnixDomainSocketEndPoint(_options.SocketPath),
                linkedSource.Token);

            using var stream = new NetworkStream(socket, ownsSocket: false);
            await LengthPrefixedJsonMessageFraming.WriteAsync(
                stream,
                request,
                linkedSource.Token);

            return await LengthPrefixedJsonMessageFraming
                .ReadAsync<InspectionExecutionResult>(
                    stream,
                    linkedSource.Token);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested &&
                  timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Inspection Request timed out after {_timeout}.",
                exception);
        }
    }
}
