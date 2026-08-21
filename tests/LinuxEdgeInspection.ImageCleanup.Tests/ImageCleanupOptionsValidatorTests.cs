using LinuxEdgeInspection.ImageCleanup.Options;

namespace LinuxEdgeInspection.ImageCleanup.Tests;

public sealed class ImageCleanupOptionsValidatorTests
{
    private readonly ImageCleanupOptionsValidator _validator = new();

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Validate_RetentionDaysが1未満の場合は失敗する(
        int retentionDays)
    {
        var options = CreateValidOptions();
        options.RetentionDays = retentionDays;

        var result = _validator.Validate(
            name: null,
            options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(
                "RetentionDays",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RetentionDaysが1の場合は成功する()
    {
        var options = CreateValidOptions();
        options.RetentionDays = 1;

        var result = _validator.Validate(
            name: null,
            options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relative/captures")]
    public void Validate_RootDirectoryが空または相対パスの場合は失敗する(
        string rootDirectory)
    {
        var options = CreateValidOptions();
        options.RootDirectory = rootDirectory;

        var result = _validator.Validate(
            name: null,
            options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains(
                "RootDirectory",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FilesystemRootの場合は失敗する()
    {
        var options = CreateValidOptions();
        options.RootDirectory =
            Path.GetPathRoot(Path.GetTempPath())!;

        var result = _validator.Validate(
            name: null,
            options);

        Assert.False(result.Succeeded);
    }

    private static ImageCleanupOptions CreateValidOptions() =>
        new()
        {
            RootDirectory = Path.Combine(
                Path.GetTempPath(),
                "linux-edge-inspection-captures"),
            RetentionDays = 7,
            DryRun = false
        };
}
