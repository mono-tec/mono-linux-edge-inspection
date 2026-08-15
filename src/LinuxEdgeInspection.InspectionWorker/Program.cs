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

builder.Services.AddSingleton<ICaptureRequestClient,
    UnixDomainSocketCaptureRequestClient>();

builder.Services.AddSingleton<InspectionWorkerService>();

var host = builder.Build();

// ------------------------------------------------------------
// 1回だけCapture Requestを送信する手動実行モード
// ------------------------------------------------------------

if (args.Contains("--capture-once"))
{
    var inspectionWorkerService =
        host.Services.GetRequiredService<InspectionWorkerService>();

    var request = new CaptureRequest(
        RequestId: Guid.NewGuid().ToString(),
        CaptureIndex: 1,
        RequestedAt: DateTimeOffset.UtcNow);

    var result = await inspectionWorkerService.CaptureAsync(
        request);

    return result.Succeeded ? 0 : 1;
}

// ------------------------------------------------------------
// 通常起動
// ------------------------------------------------------------

// Equipment Gateway実装前のため、自動Capture Requestは生成しません。
await host.RunAsync();

return 0;