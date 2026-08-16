using LinuxEdgeInspection.Management;
using LinuxEdgeInspection.Management.Components;
using LinuxEdgeInspection.Management.Core;
using LinuxEdgeInspection.Plugin.CameraTest;
using LinuxEdgeInspection.Plugin.DiskMonitor;
using LinuxEdgeInspection.Plugin.LogViewer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// OSに依存しないPluginは常に登録します。
builder.Services.AddDiskMonitorPlugin();

// Linux環境でのみ使用するPluginを登録します。
if (OperatingSystem.IsLinux())
{
    builder.Services.AddCameraTestPlugin();
    builder.Services.AddLogViewerPlugin();

    var managementApiBaseUrl =
        builder.Configuration["ManagementApi:BaseUrl"]
        ?? throw new InvalidOperationException(
            "ManagementApi:BaseUrl is required.");

    builder.Services.AddHttpClient(
        "ManagementApi",
        client =>
        {
            client.BaseAddress =
                new Uri(managementApiBaseUrl, UriKind.Absolute);
        });
}

// Hostが対象Assemblyを明示してPluginを検出します。
builder.Services.AddSingleton<PluginRegistry>(_ =>
{
    var registry = new PluginRegistry();

    var plugins =
        PluginDiscovery.Discover(PluginAssemblies.Current);

    foreach (var plugin in plugins)
    {
        registry.Add(plugin);
    }

    return registry;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

var registry =
    app.Services.GetRequiredService<PluginRegistry>();

var logger =
    app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Linux Edge Inspection Platform management host started with {PluginCount} plugin(s).",
    registry.Plugins.Count);

foreach (var plugin in registry.Plugins)
{
    logger.LogInformation(
        "Plugin enabled: Id={PluginId}, Name={PluginName}, Version={PluginVersion}, Route={PluginRoute}",
        plugin.Manifest.Id,
        plugin.Manifest.Name,
        plugin.Manifest.Version,
        plugin.Manifest.Route);
}

app.MapStaticAssets();

// Camera Test用の画像relayもLinux環境だけ登録します。
if (OperatingSystem.IsLinux())
{
    app.MapGet(
        "/inspection/images/{fileName}",
        async (
            string fileName,
            IHttpClientFactory httpClientFactory,
            CancellationToken cancellationToken) =>
        {
            var client =
                httpClientFactory.CreateClient("ManagementApi");

            using var response =
                await client.GetAsync(
                    $"api/inspection/images/{Uri.EscapeDataString(fileName)}",
                    cancellationToken);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode(
                    (int)response.StatusCode);
            }

            var image =
                await response.Content.ReadAsByteArrayAsync(
                    cancellationToken);

            return Results.File(
                image,
                "image/jpeg");
        });
}

// 実行OSで有効なPlugin AssemblyをBlazor Routerへ登録します。
// Windows / LinuxごとのPlugin切り替えはPluginAssemblies.Currentで一元管理します。
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        PluginAssemblies.Current);

app.Run();