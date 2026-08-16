using LinuxEdgeInspection.Management.Components;
using LinuxEdgeInspection.Management.Core;
using LinuxEdgeInspection.Management;
using LinuxEdgeInspection.Plugin.CameraTest;
using LinuxEdgeInspection.Plugin.DiskMonitor;
using LinuxEdgeInspection.Plugin.LogViewer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Plugin固有の処理は各Pluginが提供する拡張メソッドから登録します。
builder.Services.AddDiskMonitorPlugin();
builder.Services.AddCameraTestPlugin();
builder.Services.AddLogViewerPlugin();

// DiskMonitor基準実装と同様に、Hostが対象Assemblyを明示してPluginを検出します。
builder.Services.AddSingleton<PluginRegistry>(_ =>
{
    var registry = new PluginRegistry();
    var plugins = PluginDiscovery.Discover(PluginAssemblies.All);

    foreach (var plugin in plugins)
    {
        registry.Add(plugin);
    }

    return registry;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

var registry = app.Services.GetRequiredService<PluginRegistry>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(PluginAssemblies.All);

app.Run();
