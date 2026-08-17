using LinuxEdgeInspection.Analyzer.Services;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.InspectionWorker.Services;
using LinuxEdgeInspection.Preprocessor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });

// ------------------------------------------------------------
// Linux環境でのみ使用するサービスを登録します。
// ------------------------------------------------------------

if (OperatingSystem.IsLinux())
{
    // CaptureRequestListenerへ接続するための
    // Unix Domain Socket設定を読み込みます。
    builder.Services.Configure<CaptureRequestClientOptions>(
        builder.Configuration.GetRequiredSection(
            CaptureRequestClientOptions.SectionName));

    builder.Services.AddSingleton<
        ICaptureRequestClient,
        UnixDomainSocketCaptureRequestClient>();

    // Management APIからInspection要求を受け付けるための
    // Unix Domain Socket設定を読み込みます。
    builder.Services.Configure<InspectionRequestEndpointOptions>(
        builder.Configuration.GetRequiredSection(
            InspectionRequestEndpointOptions.SectionName));

    // Management APIからのInspection要求を
    // Unix Domain Socketで常時受け付けます。
    builder.Services.AddHostedService<
        UnixDomainSocketInspectionRequestServer>();
}

// ------------------------------------------------------------
// Inspection Pipelineで使用する共通サービスを登録します。
// ------------------------------------------------------------

builder.Services.AddSingleton<
    IPreprocessor,
    DummyPreprocessor>();

builder.Services.AddSingleton<
    IAnalyzer,
    DummyAnalyzer>();

builder.Services.AddSingleton<
    InspectionWorkerService>();

using var host =
    builder.Build();

// ------------------------------------------------------------
// Capture → Preprocess → Analyzeを1回実行する手動確認モード
// ------------------------------------------------------------

if (args.Contains("--capture-once"))
{
    var inspectionWorkerService =
        host.Services.GetRequiredService<
            InspectionWorkerService>();

    var request =
        new CaptureRequest(
            RequestId: Guid.NewGuid().ToString(),
            CaptureIndex: 1,
            RequestedAt: DateTimeOffset.UtcNow);

    var pipelineResult =
        await inspectionWorkerService.InspectOnceAsync(
            request);

    var result = pipelineResult.CaptureResult;

    // 手動実行時は撮影結果をConsoleにも表示します。
    // ILoggerとは別に出力することで、
    // CLIからFilePath等を直接確認できるようにします。
    Console.WriteLine();
    Console.WriteLine(
        "Capture Result");

    Console.WriteLine(
        $"RequestId    : {result.RequestId}");

    Console.WriteLine(
        $"CaptureIndex : {result.CaptureIndex}");

    Console.WriteLine(
        $"Succeeded    : {result.Succeeded}");

    Console.WriteLine(
        $"FilePath     : {result.FilePath}");

    Console.WriteLine(
        $"CompletedAt  : {result.CompletedAt}");

    Console.WriteLine(
        $"ErrorCode    : {result.ErrorCode}");

    Console.WriteLine(
        $"ErrorMessage : {result.ErrorMessage}");

    if (pipelineResult.PreprocessResult is not null)
    {
        Console.WriteLine();
        Console.WriteLine(
            "Preprocess Result");

        Console.WriteLine(
            $"Succeeded    : {pipelineResult.PreprocessResult.Succeeded}");

        Console.WriteLine(
            $"FilePaths    : {string.Join(", ", pipelineResult.PreprocessResult.FilePaths)}");

        Console.WriteLine(
            $"ErrorCode    : {pipelineResult.PreprocessResult.ErrorCode}");

        Console.WriteLine(
            $"ErrorMessage : {pipelineResult.PreprocessResult.ErrorMessage}");
    }

    if (pipelineResult.AnalysisResult is not null)
    {
        Console.WriteLine();
        Console.WriteLine(
            "Analysis Result");

        Console.WriteLine(
            $"Succeeded    : {pipelineResult.AnalysisResult.Succeeded}");

        Console.WriteLine(
            $"Judgement    : {pipelineResult.AnalysisResult.Judgement}");

        Console.WriteLine(
            $"Label        : {pipelineResult.AnalysisResult.Label}");

        Console.WriteLine(
            $"Score        : {pipelineResult.AnalysisResult.Score}");

        Console.WriteLine(
            $"ErrorCode    : {pipelineResult.AnalysisResult.ErrorCode}");

        Console.WriteLine(
            $"ErrorMessage : {pipelineResult.AnalysisResult.ErrorMessage}");
    }

    return result.Succeeded &&
           pipelineResult.PreprocessResult?.Succeeded == true &&
           pipelineResult.AnalysisResult?.Succeeded == true
        ? 0
        : 1;
}

// ------------------------------------------------------------
// 通常起動
// ------------------------------------------------------------

// Equipment Gateway実装前のため、
// 通常起動時は自動Capture Requestを生成しません。
// LinuxではUnixDomainSocketInspectionRequestServerが常駐し、
// Management APIからのInspection要求を待機します。
await host.RunAsync();

return 0;