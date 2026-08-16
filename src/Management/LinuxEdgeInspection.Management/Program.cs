using LinuxEdgeInspection.Plugin.CameraTest;
using LinuxEdgeInspection.Plugin.CameraTest.Services;
using LinuxEdgeInspection.Plugin.DiskMonitor;
using LinuxEdgeInspection.Plugin.LogViewer;
using LinuxEdgeInspection.Plugin.LogViewer.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// OSに依存しないPluginは常に登録します。
builder.Services.AddDiskMonitorPlugin();

// Linux環境でのみ使用するPluginを登録します。
if (OperatingSystem.IsLinux())
{
    // Plugin標準登録
    builder.Services.AddCameraTestPlugin();
    builder.Services.AddLogViewerPlugin();

    var managementApiBaseUrl =
        builder.Configuration["ManagementApi:BaseUrl"]
        ?? throw new InvalidOperationException(
            "ManagementApi:BaseUrl is required.");

    // Camera TestのDummy実装を削除し、
    // LinuxではManagement APIを呼び出す実装へ差し替えます。
    builder.Services.RemoveAll<ICameraTestService>();

    builder.Services.AddHttpClient<
        ICameraTestService,
        HttpCameraTestService>(
        client =>
        {
            client.BaseAddress =
                new Uri(managementApiBaseUrl, UriKind.Absolute);
        });

    // Log ViewerのDummy実装を削除し、
    // Linuxではjournaldを参照する実装へ差し替えます。
    builder.Services.RemoveAll<ILogViewerService>();

    builder.Services.AddSingleton<
        ILogViewerService,
        JournaldLogViewerService>();

    // Camera Testの画像relayでも使用する名前付きHttpClientです。
    builder.Services.AddHttpClient(
        "ManagementApi",
        client =>
        {
            client.BaseAddress =
                new Uri(managementApiBaseUrl, UriKind.Absolute);
        });
}