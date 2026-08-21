using LinuxEdgeInspection.ImageCleanup.Options;
using LinuxEdgeInspection.ImageCleanup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder =
    Host.CreateApplicationBuilder(
        new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

var imageCleanupSection =
    builder.Configuration.GetSection(
        ImageCleanupOptions.SectionName);

builder.Services
    .AddOptions<ImageCleanupOptions>()
    .Bind(imageCleanupSection)
    .ValidateOnStart();

builder.Services.AddSingleton<
    IValidateOptions<ImageCleanupOptions>,
    ImageCleanupOptionsValidator>();

builder.Services.AddSingleton(
    TimeProvider.System);

builder.Services.AddSingleton<
    IImageCleanupFileSystem,
    PhysicalImageCleanupFileSystem>();

builder.Services.AddSingleton<
    IImageCleanupService,
    ImageCleanupService>();

using var host = builder.Build();

var logger =
    host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("LinuxEdgeInspection.ImageCleanup");

try
{
    if (!imageCleanupSection.Exists())
    {
        logger.LogError(
            "ImageCleanup設定が不正です。設定セクションが存在しません。SectionName: {SectionName}",
            ImageCleanupOptions.SectionName);

        return 1;
    }

    var cleanupService =
        host.Services.GetRequiredService<
            IImageCleanupService>();

    var result = cleanupService.Cleanup();

    return result.FailedCount > 0 ? 1 : 0;
}
catch (OptionsValidationException exception)
{
    logger.LogError(
        exception,
        "ImageCleanup設定が不正です。Errors: {ValidationErrors}",
        string.Join("; ", exception.Failures));

    return 1;
}
catch (Exception exception)
{
    logger.LogError(
        exception,
        "ImageCleanupの実行中に予期しないエラーが発生しました。");

    return 1;
}
