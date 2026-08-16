using LinuxEdgeInspection.Contracts.Inspection;

namespace LinuxEdgeInspection.Management.Api.Services;

public interface IInspectionWorkerClient
{
    Task<InspectionExecutionResult> ExecuteAsync(
        InspectionExecutionRequest request,
        CancellationToken cancellationToken = default);
}
