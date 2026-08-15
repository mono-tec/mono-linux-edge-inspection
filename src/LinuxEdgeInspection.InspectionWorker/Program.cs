using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.InspectionWorker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CaptureRequestClientOptions>(
    builder.Configuration.GetRequiredSection(
        CaptureRequestClientOptions.SectionName));

builder.Services.AddSingleton<ICaptureRequestClient,
    UnixDomainSocketCaptureRequestClient>();
builder.Services.AddSingleton<InspectionWorkerService>();

// Equipment Gateway実装前のため、自動Capture Requestは生成しません。
await builder.Build().RunAsync();
