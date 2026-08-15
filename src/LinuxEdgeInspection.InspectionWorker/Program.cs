using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.InspectionWorker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });

builder.Services.Configure<CaptureRequestClientOptions>(
    builder.Configuration.GetRequiredSection(
        CaptureRequestClientOptions.SectionName));

builder.Services.AddSingleton<
    ICaptureRequestClient,
    UnixDomainSocketCaptureRequestClient>();

builder.Services.AddSingleton<
    InspectionWorkerService>();

using var host =
    builder.Build();

// ------------------------------------------------------------
// 1回だけCapture Requestを送信する手動実行モード
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

    var result =
        await inspectionWorkerService.CaptureAsync(
            request);

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

    return result.Succeeded
        ? 0
        : 1;
}

// ------------------------------------------------------------
// 通常起動
// ------------------------------------------------------------

// Equipment Gateway実装前のため、
// 通常起動時は自動Capture Requestを生成しません。
await host.RunAsync();

return 0;