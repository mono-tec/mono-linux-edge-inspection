using System.Text.Json.Serialization;
using LinuxEdgeInspection.Management.Api.Endpoints;
using LinuxEdgeInspection.Management.Api.Options;
using LinuxEdgeInspection.Management.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<InspectionWorkerClientOptions>(
    builder.Configuration.GetRequiredSection(
        InspectionWorkerClientOptions.SectionName));

builder.Services.Configure<CaptureImageOptions>(
    builder.Configuration.GetRequiredSection(
        CaptureImageOptions.SectionName));

builder.Services.AddSingleton<IInspectionWorkerClient,
    UnixDomainSocketInspectionWorkerClient>();

builder.Services.AddSingleton<ICaptureImageFileSystem,
    PhysicalCaptureImageFileSystem>();

builder.Services.AddSingleton<CaptureImageService>();

var app = builder.Build();

// Development環境のみOpenAPI / Swagger UIを有効化
//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/openapi/v1.json",
        "Linux Edge Inspection Management API v1");
});
//}

app.MapInspectionEndpoints();

app.Run();

public partial class Program;