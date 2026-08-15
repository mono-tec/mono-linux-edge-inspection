using LinuxEdgeInspection.CaptureRequestListener.Options;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.CaptureRequestListener.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder =
    Host.CreateApplicationBuilder(args);

Console.WriteLine(
    $"Environment: {builder.Environment.EnvironmentName}");

//
// Capture Runtime起動設定
//
// appsettings.jsonのCaptureRuntimeLauncherセクションを読み込みます。
// systemctlのパス、起動対象のsystemd Unit名、
// Runtime起動処理のタイムアウト時間を保持します。
//
builder.Services.Configure<
    CaptureRuntimeLauncherOptions>(
    builder.Configuration.GetRequiredSection(
        "CaptureRuntimeLauncher"));

builder.Services.Configure<CaptureRequestEndpointOptions>(
    builder.Configuration.GetRequiredSection(
        CaptureRequestEndpointOptions.SectionName));

//
// Capture Request Queue
//
// CaptureRequestListenerが処理する撮影要求を
// メモリ上のQueueとしてFIFO順に保持します。
//
// 開発・検証時:
//   FakeCaptureRequestGeneratorWorker
//       ↓
//   CaptureRequestQueue
//
// 将来:
//   Inspection Worker
//       ↓ IPC等
//   CaptureRequestListener
//       ↓
//   CaptureRequestQueue
//
// Queueは複数のHostedServiceから共有するため、
// Singletonとして登録します。
//
builder.Services.AddSingleton<
    ICaptureRequestQueue,
    CaptureRequestQueue>();

//
// 外部システムコマンド実行サービス
//
// systemctlなどの外部コマンドを起動し、
// 以下の実行結果を取得します。
//
// ・終了コード
// ・標準出力
// ・標準エラー
// ・タイムアウト
// ・キャンセル
//
builder.Services.AddSingleton<
    ISystemCommandRunner,
    SystemCommandRunner>();


//
// Capture Runtime結果読込サービス
//
// Runtimeが出力したcapture-result.jsonを読み込みます。
//
builder.Services.AddSingleton<ICaptureRuntimeResultReader>(
    serviceProvider =>
    {
        var options =
            serviceProvider.GetRequiredService<
                IOptions<CaptureRuntimeLauncherOptions>>()
                .Value;

        return new CaptureRuntimeResultReader(
            options.ResultFilePath);
    });


//
// Capture Runtime起動サービス
//
// systemctl restartを使用して、
// Capture Runtimeのsystemd Unitを1回起動します。
//
// 起動対象のUnit名やsystemctlのパスは、
// CaptureRuntimeLauncherOptionsから取得します。
//
// OptionsとISystemCommandRunnerを使用して
// SystemdCaptureRuntimeLauncherを生成するため、
// Factory形式でDI登録します。
//
builder.Services.AddSingleton<ICaptureRuntimeLauncher>(
    serviceProvider =>
    {
        var commandRunner =
            serviceProvider.GetRequiredService<
                ISystemCommandRunner>();

        var resultReader =
            serviceProvider.GetRequiredService<
                ICaptureRuntimeResultReader>();

        var options =
            serviceProvider.GetRequiredService<
                IOptions<CaptureRuntimeLauncherOptions>>()
                .Value;

        return new SystemdCaptureRuntimeLauncher(
            commandRunner,
            resultReader,
            options.SystemctlPath,
            options.ServiceName,
            TimeSpan.FromSeconds(
                options.TimeoutSeconds));
    });

//
// Capture Request処理サービス
//
// Queueから取り出されたCapture Requestを
// 1件ずつ処理します。
//
// 処理内容:
// 1. Capture Runtimeを起動
// 2. Runtime起動結果をCaptureResultへ変換
// 3. CaptureResultを呼び出し元へ返却
//
// CaptureResultはUnix Domain Socketを介して
// Inspection Workerへ返却されます.
//
builder.Services.AddSingleton<
    ICaptureRequestProcessor,
    CaptureRequestProcessor>();

//
// Capture Request処理Worker
//
// CaptureRequestQueueを常時監視し、
// Capture RequestをFIFO順に1件ずつ処理します。
//
// 1件の処理完了後に次の要求へ進むことで、
// Capture Runtimeが同時に複数起動されることを防ぎます。
//
builder.Services.AddHostedService<
    CaptureRequestWorker>();

builder.Services.AddHostedService<
    UnixDomainSocketCaptureRequestServer>();

//
// Fake Capture Request生成Worker
//
// Development環境でのみ使用します。
//
// RequestIdを持つ疑似Capture Requestを自動生成し、
// CaptureRequestQueueへ直接追加します。
//
// 実PLCを模擬するものではなく、
// CaptureRequestListener内部の
//
// Queue
//   ↓
// Worker
//   ↓
// Processor
//   ↓
// Runtime
//
// という処理経路を確認するための機能です。
//
// Production環境では登録しません。
//
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<
        FakeCaptureRequestGeneratorOptions>(
        builder.Configuration.GetRequiredSection(
            "FakeCaptureRequestGenerator"));

    builder.Services.AddHostedService<
        FakeCaptureRequestGeneratorWorker>();
}

//
// DIコンテナとHosted Serviceを構築します。
//
var host = builder.Build();

//
// CaptureRequestListenerを常駐サービスとして起動します。
//
// systemd等から停止要求を受信した場合は、
// CancellationTokenを介して各BackgroundServiceへ
// 停止要求が通知されます。
//
await host.RunAsync();
