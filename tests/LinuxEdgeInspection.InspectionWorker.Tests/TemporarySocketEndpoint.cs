namespace LinuxEdgeInspection.InspectionWorker.Tests;

internal sealed class TemporarySocketEndpoint : IDisposable
{
    public TemporarySocketEndpoint()
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            $"lei-{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
        SocketPath = Path.Combine(DirectoryPath, "capture.sock");
    }

    public string DirectoryPath { get; }

    public string SocketPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
