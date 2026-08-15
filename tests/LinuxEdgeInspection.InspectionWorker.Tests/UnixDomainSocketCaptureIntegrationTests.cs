using LinuxEdgeInspection.CaptureRequestListener.Options;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.CaptureRequestListener.Workers;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.InspectionWorker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.InspectionWorker.Tests;

public sealed class UnixDomainSocketCaptureIntegrationTests
{
    [Fact]
    public async Task ClientToListener_RoundTripsThroughRealQueueWorkerAndProcessor()
    {
        using var endpoint = new TemporarySocketEndpoint();
        var launcher = new FakeRuntimeLauncher();
        var services = CreateServices(endpoint.SocketPath, launcher);

        await services.Worker.StartAsync(CancellationToken.None);
        await services.Server.StartAsync(CancellationToken.None);
        await services.Server.Ready.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            var request = new CaptureRequest(
                "REQ-INTEGRATION-001", 1, DateTimeOffset.Now);

            var result = await services.Client.SendAsync(request)
                .WaitAsync(TimeSpan.FromSeconds(10));

            Assert.True(result.Succeeded);
            Assert.Equal(request.RequestId, result.RequestId);
            Assert.Equal(request.CaptureIndex, result.CaptureIndex);
            Assert.Equal(1, launcher.LaunchCount);
        }
        finally
        {
            await services.Server.StopAsync(CancellationToken.None);
            await services.Worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task MultipleClients_AreProcessedWithoutConcurrentRuntimeLaunches()
    {
        using var endpoint = new TemporarySocketEndpoint();
        var launcher = new FakeRuntimeLauncher(
            TimeSpan.FromMilliseconds(50));
        var services = CreateServices(endpoint.SocketPath, launcher);

        await services.Worker.StartAsync(CancellationToken.None);
        await services.Server.StartAsync(CancellationToken.None);
        await services.Server.Ready.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            var tasks = Enumerable.Range(1, 3)
                .Select(index => services.Client.SendAsync(
                    new CaptureRequest(
                        $"REQ-{index}", 1, DateTimeOffset.Now)))
                .ToArray();

            var results = await Task.WhenAll(tasks)
                .WaitAsync(TimeSpan.FromSeconds(10));

            Assert.All(results, result => Assert.True(result.Succeeded));
            Assert.Equal(3, launcher.LaunchCount);
            Assert.Equal(1, launcher.MaximumConcurrency);
        }
        finally
        {
            await services.Server.StopAsync(CancellationToken.None);
            await services.Worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Server_WhenRequestIsInvalid_ReturnsValidationFailure()
    {
        using var endpoint = new TemporarySocketEndpoint();
        var queue = new CaptureRequestQueue();
        var server = CreateServer(endpoint.SocketPath, queue);
        var client = CreateClient(endpoint.SocketPath);

        await server.StartAsync(CancellationToken.None);
        await server.Ready.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            var result = await client.SendAsync(
                new CaptureRequest(string.Empty, 0, DateTimeOffset.Now));

            Assert.False(result.Succeeded);
            Assert.Equal(
                UnixDomainSocketCaptureRequestServer.InvalidRequestErrorCode,
                result.ErrorCode);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    private static ServiceSet CreateServices(
        string socketPath,
        ICaptureRuntimeLauncher launcher)
    {
        var queue = new CaptureRequestQueue();
        var processor = new CaptureRequestProcessor(launcher);
        return new ServiceSet(
            CreateServer(socketPath, queue),
            new CaptureRequestWorker(
                queue,
                processor,
                NullLogger<CaptureRequestWorker>.Instance),
            CreateClient(socketPath));
    }

    private static UnixDomainSocketCaptureRequestServer CreateServer(
        string socketPath,
        ICaptureRequestQueue queue) =>
        new(
            queue,
            Microsoft.Extensions.Options.Options.Create(
                new CaptureRequestEndpointOptions
            {
                SocketPath = socketPath,
                Backlog = 16
                }),
            NullLogger<UnixDomainSocketCaptureRequestServer>.Instance);

    private static UnixDomainSocketCaptureRequestClient CreateClient(
        string socketPath) =>
        new(Microsoft.Extensions.Options.Options.Create(
            new CaptureRequestClientOptions
        {
            SocketPath = socketPath,
            TimeoutSeconds = 5
            }));

    private sealed record ServiceSet(
        UnixDomainSocketCaptureRequestServer Server,
        CaptureRequestWorker Worker,
        UnixDomainSocketCaptureRequestClient Client);

    private sealed class FakeRuntimeLauncher : ICaptureRuntimeLauncher
    {
        private readonly TimeSpan _delay;
        private int _launchCount;
        private int _concurrency;
        private int _maximumConcurrency;

        public FakeRuntimeLauncher(TimeSpan delay = default) =>
            _delay = delay;

        public int LaunchCount => _launchCount;

        public int MaximumConcurrency => _maximumConcurrency;

        public async Task<LinuxEdgeInspection.CaptureRequestListener.Models.CaptureRuntimeLaunchResult>
            LaunchAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _launchCount);
            var concurrency = Interlocked.Increment(ref _concurrency);
            UpdateMaximumConcurrency(concurrency);
            var startedAt = DateTimeOffset.Now;

            try
            {
                if (_delay > TimeSpan.Zero)
                {
                    await Task.Delay(_delay, cancellationToken);
                }

                return new(
                    Succeeded: true,
                    ExitCode: 0,
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.Now,
                    FilePath: null,
                    ErrorCode: null,
                    ErrorMessage: null);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrency);
            }
        }

        private void UpdateMaximumConcurrency(int value)
        {
            int current;
            do
            {
                current = Volatile.Read(ref _maximumConcurrency);
                if (current >= value)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                ref _maximumConcurrency,
                value,
                current) != current);
        }
    }
}
