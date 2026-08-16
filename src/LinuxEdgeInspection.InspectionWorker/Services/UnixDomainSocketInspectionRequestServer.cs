using System.Net.Sockets;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Contracts.Inspection;
using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.Ipc.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.InspectionWorker.Services;

/// <summary>
/// Management APIからInspection要求を受信し、既存Pipelineを直列実行します。
/// </summary>
public sealed class UnixDomainSocketInspectionRequestServer : BackgroundService
{
    public const string InvalidRequestErrorCode = "INSPECTION_REQUEST_INVALID";
    public const string ExecutionErrorCode = "INSPECTION_EXECUTION_FAILED";

    private readonly InspectionWorkerService _inspectionWorkerService;
    private readonly InspectionRequestEndpointOptions _options;
    private readonly ILogger<UnixDomainSocketInspectionRequestServer> _logger;
    private readonly TaskCompletionSource _readySource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Socket? _listener;

    public Task Ready => _readySource.Task;

    public UnixDomainSocketInspectionRequestServer(
        InspectionWorkerService inspectionWorkerService,
        IOptions<InspectionRequestEndpointOptions> options,
        ILogger<UnixDomainSocketInspectionRequestServer> logger)
    {
        _inspectionWorkerService = inspectionWorkerService
            ?? throw new ArgumentNullException(nameof(inspectionWorkerService));
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            "Inspection Request Socketを開始しました。SocketPath: {SocketPath}",
            _options.SocketPath);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var socket = await _listener.AcceptAsync(stoppingToken);
                await HandleConnectionAsync(socket, stoppingToken);
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
            DeleteSocketFileIfPresent();
            _logger.LogInformation("Inspection Request Socketを停止しました。");
        }
    }

    private async Task HandleConnectionAsync(
        Socket socket,
        CancellationToken stoppingToken)
    {
        using var stream = new NetworkStream(socket, ownsSocket: false);
        InspectionExecutionRequest? request = null;

        try
        {
            request = await LengthPrefixedJsonMessageFraming
                .ReadAsync<InspectionExecutionRequest>(stream, stoppingToken);

            if (!IsValid(request))
            {
                await WriteFailureAsync(
                    stream,
                    request,
                    InvalidRequestErrorCode,
                    "RequestId must not be empty and CaptureIndex must be at least 1.",
                    stoppingToken);
                return;
            }

            var pipelineResult = await _inspectionWorkerService.InspectOnceAsync(
                new CaptureRequest(
                    request.RequestId,
                    request.CaptureIndex,
                    request.RequestedAt),
                stoppingToken);

            await LengthPrefixedJsonMessageFraming.WriteAsync(
                stream,
                new InspectionExecutionResult(
                    pipelineResult.CaptureResult,
                    pipelineResult.PreprocessResult,
                    pipelineResult.AnalysisResult),
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
                "Inspection Request Socket接続の処理に失敗しました。");

            if (request is not null && stream.CanWrite)
            {
                try
                {
                    await WriteFailureAsync(
                        stream,
                        request,
                        ExecutionErrorCode,
                        "Inspection Pipelineの実行に失敗しました。",
                        stoppingToken);
                }
                catch (Exception writeException)
                    when (writeException is IOException or SocketException)
                {
                    _logger.LogDebug(
                        writeException,
                        "Inspection失敗応答を送信できませんでした。");
                }
            }
        }
    }

    private static bool IsValid(InspectionExecutionRequest request) =>
        !string.IsNullOrWhiteSpace(request.RequestId) &&
        request.CaptureIndex >= 1;

    private static ValueTask WriteFailureAsync(
        Stream stream,
        InspectionExecutionRequest request,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken) =>
        LengthPrefixedJsonMessageFraming.WriteAsync(
            stream,
            new InspectionExecutionResult(
                new CaptureResult(
                    request.RequestId ?? string.Empty,
                    request.CaptureIndex,
                    Succeeded: false,
                    CompletedAt: DateTimeOffset.UtcNow,
                    FilePath: null,
                    ErrorCode: errorCode,
                    ErrorMessage: errorMessage),
                PreprocessResult: null,
                AnalysisResult: null),
            cancellationToken);

    private void DeleteSocketFileIfPresent()
    {
        if (File.Exists(_options.SocketPath))
        {
            File.Delete(_options.SocketPath);
        }
    }
}
