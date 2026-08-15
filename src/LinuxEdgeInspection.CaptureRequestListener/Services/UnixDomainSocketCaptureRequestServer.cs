using System.Collections.Concurrent;
using System.Net.Sockets;
using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Options;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Ipc.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Unix Domain SocketからCapture Requestを1件受信し、対応するResultを返します。
/// </summary>
public sealed class UnixDomainSocketCaptureRequestServer : BackgroundService
{
    public const string InvalidRequestErrorCode = "CAPTURE_REQUEST_INVALID";

    private readonly ICaptureRequestQueue _requestQueue;
    private readonly CaptureRequestEndpointOptions _options;
    private readonly ILogger<UnixDomainSocketCaptureRequestServer> _logger;
    private readonly ConcurrentDictionary<int, Task> _connections = new();
    private readonly TaskCompletionSource _readySource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _connectionId;
    private Socket? _listener;

    public Task Ready => _readySource.Task;

    public UnixDomainSocketCaptureRequestServer(
        ICaptureRequestQueue requestQueue,
        IOptions<CaptureRequestEndpointOptions> options,
        ILogger<UnixDomainSocketCaptureRequestServer> logger)
    {
        _requestQueue = requestQueue
            ?? throw new ArgumentNullException(nameof(requestQueue));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value
            ?? throw new ArgumentNullException(nameof(options));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        ArgumentException.ThrowIfNullOrWhiteSpace(_options.SocketPath);
        if (_options.Backlog <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Backlog must be greater than zero.");
        }
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var directory = Path.GetDirectoryName(_options.SocketPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException(
                "Socket path must include a directory.");
        }

        Directory.CreateDirectory(directory);
        DeleteSocketFileIfPresent();

        _listener = new Socket(
            AddressFamily.Unix,
            SocketType.Stream,
            ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(_options.SocketPath));
        _listener.Listen(_options.Backlog);

        _readySource.TrySetResult();

        _logger.LogInformation(
            "Capture Request Socketを開始しました。SocketPath: {SocketPath}",
            _options.SocketPath);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var socket = await _listener.AcceptAsync(stoppingToken);
                var id = Interlocked.Increment(ref _connectionId);
                var task = HandleConnectionAsync(socket, stoppingToken);
                _connections.TryAdd(id, task);
                _ = task.ContinueWith(
                    completedTask =>
                        _connections.TryRemove(id, out var ignoredTask),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal service shutdown.
        }
        finally
        {
            _listener.Dispose();
            _listener = null;

            await Task.WhenAll(_connections.Values);
            DeleteSocketFileIfPresent();

            _logger.LogInformation("Capture Request Socketを停止しました。");
        }
    }

    private async Task HandleConnectionAsync(
        Socket socket,
        CancellationToken stoppingToken)
    {
        using (socket)
        using (var stream = new NetworkStream(socket, ownsSocket: false))
        {
            try
            {
                var request =
                    await LengthPrefixedJsonMessageFraming.ReadAsync<CaptureRequest>(
                        stream,
                        stoppingToken);

                if (!IsValid(request))
                {
                    await WriteInvalidRequestAsync(stream, request, stoppingToken);
                    return;
                }

                var item = new CaptureRequestQueueItem(request);
                await _requestQueue.EnqueueAsync(item, stoppingToken);

                var result = await item.Completion.WaitAsync(stoppingToken);
                await LengthPrefixedJsonMessageFraming.WriteAsync(
                    stream,
                    result,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal service shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Capture Request Socket接続の処理に失敗しました。");
            }
        }
    }

    private static bool IsValid(CaptureRequest request) =>
        !string.IsNullOrWhiteSpace(request.RequestId) &&
        request.CaptureIndex >= 1;

    private static ValueTask WriteInvalidRequestAsync(
        Stream stream,
        CaptureRequest request,
        CancellationToken cancellationToken) =>
        LengthPrefixedJsonMessageFraming.WriteAsync(
            stream,
            new CaptureResult(
                RequestId: request.RequestId ?? string.Empty,
                CaptureIndex: request.CaptureIndex,
                Succeeded: false,
                CompletedAt: DateTimeOffset.Now,
                FilePath: null,
                ErrorCode: InvalidRequestErrorCode,
                ErrorMessage:
                    "RequestId must not be empty and CaptureIndex must be at least 1."),
            cancellationToken);

    private void DeleteSocketFileIfPresent()
    {
        if (File.Exists(_options.SocketPath))
        {
            File.Delete(_options.SocketPath);
        }
    }
}
