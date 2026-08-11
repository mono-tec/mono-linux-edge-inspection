using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.Abstractions.Services;
using LinuxEdgeInspection.Camera.V4L2.Services;
using LinuxEdgeInspection.Runtime.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var cameraOptions =
    builder.Configuration
        .GetRequiredSection("Camera")
        .Get<CameraOptions>()
    ?? throw new InvalidOperationException(
        "appsettings.jsonのCamera設定を読み込めませんでした。");

builder.Services.AddSingleton(cameraOptions);

builder.Services.AddSingleton<
    IV4L2CommandBuilder,
    V4L2CommandBuilder>();

builder.Services.AddSingleton<
    ICameraFileService,
    CameraFileService>();

builder.Services.AddSingleton<
    ICameraProcessRunner,
    CameraProcessRunner>();

builder.Services.AddSingleton<
    ICameraDeviceFileSystem,
    CameraDeviceFileSystem>();

builder.Services.AddSingleton<
    ICameraEnvironmentService,
    CameraEnvironmentService>();

builder.Services.AddSingleton<
    ICameraStateManager,
    CameraStateManager>();

builder.Services.AddSingleton<
    ICameraService,
    V4L2CameraService>();

builder.Services.AddSingleton<
    ICameraRuntimeService,
    CameraRuntimeService>();

using var host = builder.Build();

var runtimeService =
    host.Services.GetRequiredService<ICameraRuntimeService>();

await runtimeService.RunAsync();