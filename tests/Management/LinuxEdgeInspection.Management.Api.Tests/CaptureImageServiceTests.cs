using LinuxEdgeInspection.Management.Api.Endpoints;
using LinuxEdgeInspection.Management.Api.Options;
using LinuxEdgeInspection.Management.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Management.Api.Tests;

public sealed class CaptureImageServiceTests
{
    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../outside.jpg")]
    [InlineData("sub/image.jpg")]
    [InlineData("sub\\image.jpg")]
    [InlineData("image.png")]
    [InlineData("image")]
    public void Open_RejectsInvalidOrTraversalFileName(string fileName)
    {
        var service = CreateService(new FakeFileSystem());

        var result = service.Open(fileName);

        Assert.Equal(CaptureImageOpenStatus.InvalidFileName, result.Status);
    }

    [Fact]
    public void Open_WhenFileDoesNotExist_ReturnsNotFound()
    {
        var fileSystem = new FakeFileSystem { FileExistsResult = false };
        var service = CreateService(fileSystem);

        var result = service.Open("missing.jpg");

        Assert.Equal(CaptureImageOpenStatus.NotFound, result.Status);
    }

    [Fact]
    public void Open_RejectsFileSymbolicLink()
    {
        var fileSystem = new FakeFileSystem { FileIsSymbolicLink = true };
        var service = CreateService(fileSystem);

        var result = service.Open("outside.jpg");

        Assert.Equal(
            CaptureImageOpenStatus.SymbolicLinkRejected,
            result.Status);
    }

    [Fact]
    public void Open_RejectsCapturesDirectorySymbolicLink()
    {
        var fileSystem = new FakeFileSystem
        {
            DirectoryIsSymbolicLink = true
        };
        var service = CreateService(fileSystem);

        var result = service.Open("outside.jpg");

        Assert.Equal(
            CaptureImageOpenStatus.SymbolicLinkRejected,
            result.Status);
    }

    [Fact]
    public async Task GetCaptureImage_WhenMissing_Returns404()
    {
        var service = CreateService(new FakeFileSystem
        {
            FileExistsResult = false
        });
        var result = InspectionEndpoints.GetCaptureImage("missing.jpg", service);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };

        await result.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task GetCaptureImage_ReturnsJpegContent()
    {
        byte[] jpeg = [0xff, 0xd8, 0xff, 0xd9];
        var fileSystem = new FakeFileSystem { Content = jpeg };
        var service = CreateService(fileSystem);
        var result = InspectionEndpoints.GetCaptureImage("capture.JPEG", service);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("image/jpeg", context.Response.ContentType);
        Assert.Equal(jpeg, ((MemoryStream)context.Response.Body).ToArray());
    }

    private static CaptureImageService CreateService(
        ICaptureImageFileSystem fileSystem) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new CaptureImageOptions
            {
                RootDirectory = Path.Combine(
                    Path.GetTempPath(),
                    "lei-capture-tests")
            }),
            fileSystem);

    private sealed class FakeFileSystem : ICaptureImageFileSystem
    {
        public bool FileExistsResult { get; init; } = true;
        public bool FileIsSymbolicLink { get; init; }
        public bool DirectoryIsSymbolicLink { get; init; }
        public byte[] Content { get; init; } = [1, 2, 3];

        public bool DirectoryExists(string path) => true;

        public bool FileExists(string path) => FileExistsResult;

        public bool IsSymbolicLink(string path, bool directory) =>
            directory ? DirectoryIsSymbolicLink : FileIsSymbolicLink;

        public Stream OpenRead(string path) => new MemoryStream(Content);
    }
}
