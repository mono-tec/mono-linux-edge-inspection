using System.Net.Sockets;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.InspectionWorker.Services;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.InspectionWorker.Tests;

public sealed class UnixDomainSocketCaptureRequestClientTests
{
    [Fact]
    public async Task SendAsync_WhenResponseTimesOut_ThrowsTimeoutException()
    {
        using var endpoint = new TemporarySocketEndpoint();
        using var listener = CreateListener(endpoint.SocketPath);
        var acceptTask = listener.AcceptAsync(CancellationToken.None).AsTask();
        var client = CreateClient(endpoint.SocketPath, timeoutSeconds: 1);

        var sendTask = client.SendAsync(CreateRequest());
        using var accepted = await acceptTask.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<TimeoutException>(() => sendTask);
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var endpoint = new TemporarySocketEndpoint();
        var client = CreateClient(endpoint.SocketPath, timeoutSeconds: 10);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.SendAsync(CreateRequest(), source.Token));
    }

    [Fact]
    public async Task SendAsync_WhenSocketDoesNotExist_ThrowsSocketException()
    {
        using var endpoint = new TemporarySocketEndpoint();
        var client = CreateClient(endpoint.SocketPath, timeoutSeconds: 10);

        await Assert.ThrowsAsync<SocketException>(
            () => client.SendAsync(CreateRequest()));
    }

    private static UnixDomainSocketCaptureRequestClient CreateClient(
        string socketPath,
        int timeoutSeconds) =>
        new(Microsoft.Extensions.Options.Options.Create(
            new CaptureRequestClientOptions
        {
            SocketPath = socketPath,
            TimeoutSeconds = timeoutSeconds
            }));

    private static Socket CreateListener(string socketPath)
    {
        var socket = new Socket(
            AddressFamily.Unix,
            SocketType.Stream,
            ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));
        socket.Listen(1);
        return socket;
    }

    private static CaptureRequest CreateRequest() =>
        new("REQ-001", 1, DateTimeOffset.Now);
}
