using LinuxEdgeInspection.Management.Abstractions;

namespace LinuxEdgeInspection.Plugin.CameraTest;

public sealed class CameraTestPlugin : PluginBase<CameraTestPlugin>
{
    protected override string Name => "Camera Test";

    protected override string Description => "Runs an inspection camera connectivity test.";

    protected override PluginIcon Icon => PluginIcon.CameraTest;
}
