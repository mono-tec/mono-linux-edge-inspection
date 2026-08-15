# Linux Edge Inspection

Linux環境上でカメラ撮影や検査処理を実行するための、  
エッジ向け検査プラットフォームのOSSサンプルです。

本Repositoryでは、設備やセンサーからの撮影要求を受け取り、  
Linux上のUSBカメラを使用して画像を取得し、  
前処理・分析までを段階的に実行するための基本的な実行基盤を検証・実装しています。

> [!NOTE]
> 本プロジェクトは現在開発中です。  
> 現在は、Capture Requestの受付からUSBカメラ撮影、画像前処理、分析までをつなぐ  
> 最小Pipelineを中心に実装・検証しています。

---

## ■ 現在の構成

現在は、主に以下のコンポーネントで構成されています。

```text
InspectionWorker
      │
      ▼
CaptureRequestListener
      │
      ▼
systemd
      │
      ▼
Runtime
      │
      ▼
Camera.Abstractions
      │
      ▼
Camera.V4L2
      │
      ▼
USB Camera
      │
      ▼
Image File
      │
      ▼
Preprocessor
      │
      ▼
Analyzer
      │
      ▼
Analysis Result
```

現在の最小Pipelineは以下です。

```text
Capture
   ↓
Preprocess
   ↓
Analyze
```

現在、PreprocessorおよびAnalyzerは独立Projectとして実装していますが、  
独立ProcessではなくInspectionWorker Process内で利用します。

---

### LinuxEdgeInspection.InspectionWorker

検査処理全体の流れを制御するWorkerです。

現在は、1回のCapture Requestに対して以下を順番に実行します。

```text
Capture
   ↓ 成功時のみ
Preprocess
   ↓ 成功時のみ
Analyze
```

主な役割：

- Capture Requestの送信
- Capture Resultの取得
- Preprocessorの呼び出し
- AnalysisRequestの生成
- Analyzerの呼び出し
- 各処理失敗時の後続処理停止
- Pipeline結果のログ出力

現在のWorker制御は1 Captureです。

複数Captureや正式なInspection State管理は今後の検討対象です。

---

### LinuxEdgeInspection.CaptureRequestListener

撮影要求を受け付け、FIFO順にRuntimeを起動するWorker Serviceです。

主な役割：

- Capture Requestの受付
- Capture Requestのキューイング
- FIFO順での逐次処理
- Capture同時実行の防止
- systemd経由でRuntimeを実行
- Runtime実行結果の取得
- Capture Resultの返却

現在、InspectionWorkerとの通信にはUnix Domain Socketを使用します。

```text
/run/linux-edge-inspection/capture-request-listener.sock
```

Capture Request Listenerは、

- Inspection全体の進行
- Capture回数
- 次Captureの判断
- Equipment制御
- Preprocess / Analyzeの開始判断

を担当しません。

---

### LinuxEdgeInspection.Runtime

1回の起動につき1回のカメラ撮影を実行する、  
one-shot形式のコンソールアプリケーションです。

主な役割：

- カメラ利用環境の確認
- V4L2を使用したUSBカメラ撮影
- 撮影画像の保存
- 撮影結果の出力

現在のCapture画像は以下へ保存します。

```text
/var/lib/linux-edge-inspection-runtime/captures/
```

Runtime内部の撮影結果は以下へ出力します。

```text
/var/lib/linux-edge-inspection-runtime/capture-result.json
```

---

### LinuxEdgeInspection.Preprocessor

画像前処理を担当するコンポーネントです。

現在はPipeline成立確認用としてDummyPreprocessorを実装しています。

現在のInterface：

```text
CaptureResult.FilePath
      ↓
IPreprocessor
      ↓
PreprocessResult.FilePaths
```

現在のDummyPreprocessorは、入力画像の存在を確認し、  
成功時は入力画像Pathをそのまま出力します。

実際の画像加工は今後の検討対象です。

---

### LinuxEdgeInspection.Analyzer

画像の分析・判定を担当するコンポーネントです。

現在はPipeline成立確認用としてDummyAnalyzerを実装しています。

現在の流れ：

```text
PreprocessResult.FilePaths
      ↓
InspectionWorker
      ↓
AnalysisRequest
      ↓
IAnalyzer
      ↓
AnalysisResult
```

AnalyzerはPreprocessResultそのものを受け取らず、  
分析に必要な画像PathだけをAnalysisRequestとして受け取ります。

現在のDummyAnalyzerは固定の判定結果を返します。

実AIモデルやRule-based Analysisは今後の検討対象です。

---

## ■ Solution構成

```text
mono-linux-edge-inspection/
├─ src/
│  ├─ Camera/
│  │  ├─ LinuxEdgeInspection.Camera.Abstractions/
│  │  └─ LinuxEdgeInspection.Camera.V4L2/
│  │
│  ├─ LinuxEdgeInspection.Contracts/
│  ├─ LinuxEdgeInspection.Runtime/
│  ├─ LinuxEdgeInspection.CaptureRequestListener/
│  ├─ LinuxEdgeInspection.InspectionWorker/
│  ├─ LinuxEdgeInspection.Preprocessor/
│  └─ LinuxEdgeInspection.Analyzer/
│
├─ tests/
│  ├─ Camera/
│  │  └─ LinuxEdgeInspection.Camera.V4L2.Tests/
│  │
│  ├─ LinuxEdgeInspection.Runtime.Tests/
│  ├─ LinuxEdgeInspection.CaptureRequestListener.Tests/
│  ├─ LinuxEdgeInspection.InspectionWorker.Tests/
│  ├─ LinuxEdgeInspection.Preprocessor.Tests/
│  └─ LinuxEdgeInspection.Analyzer.Tests/
│
├─ packaging/
│  ├─ runtime-deb/
│  ├─ capture-request-listener-deb/
│  └─ inspection-worker-deb/
│
├─ scripts/
│  └─ sbom/
│
├─ artifacts/
│  └─ sbom/
│
├─ LinuxEdgeInspection.slnx
├─ LICENSE
└─ README.md
```

---

## ■ 動作環境

現在の主要な検証対象は以下です。

- Linux
- x64 / amd64
- .NET 10
- systemd
- V4L2対応USBカメラ
- `v4l-utils`

GitHub Actionsでは `linux-x64` 向けにframework-dependent形式でpublishします。

そのため、実行環境には.NET 10 Runtimeが必要です。

---

## ■ Build

Repositoryルートで以下を実行します。

```bash
dotnet restore
dotnet build LinuxEdgeInspection.slnx --configuration Release
```

---

## ■ Test

Solution全体のUnit Testは以下で実行できます。

```bash
dotnet test LinuxEdgeInspection.slnx --configuration Release
```

---

## ■ Runtimeの実行

Runtimeは1回の起動につき1回の撮影を行います。

```bash
dotnet run \
  --project src/LinuxEdgeInspection.Runtime/LinuxEdgeInspection.Runtime.csproj
```

カメラ設定は `appsettings.json` から読み込みます。

---

## ■ Capture Request Listenerの実行

```bash
dotnet run \
  --project src/LinuxEdgeInspection.CaptureRequestListener/LinuxEdgeInspection.CaptureRequestListener.csproj
```

Production環境では、InspectionWorkerからUnix Domain Socket経由でCapture Requestを受け取ります。

---

## ■ Inspection Workerの手動実行

InspectionWorkerでは、Pipeline確認用として `--capture-once` を使用できます。

```bash
dotnet run \
  --project src/LinuxEdgeInspection.InspectionWorker/LinuxEdgeInspection.InspectionWorker.csproj \
  -- --capture-once
```

現在の `--capture-once` では、以下を1回実行します。

```text
Capture
   ↓
Preprocess
   ↓
Analyze
```

Consoleには以下のResultを出力します。

```text
Capture Result
Preprocess Result
Analysis Result
```

Production環境の通常起動では、Equipment Gateway未実装のため、  
InspectionWorker自身から自動的にCapture Requestを生成しません。

---

## ■ Debian Package

GitHub Actionsでは以下のDebian Packageを生成します。

```text
linux-edge-inspection-runtime_<version>_amd64.deb

linux-edge-inspection-capture-request-listener_<version>_amd64.deb

linux-edge-inspection-inspection-worker_<version>_amd64.deb
```

PreprocessorおよびAnalyzerは現在InspectionWorker Process内で利用するため、  
独立したdeb Packageとして配布しません。

Runtimeパッケージには主に以下が含まれます。

```text
/opt/linux-edge-inspection-runtime/
/usr/bin/linux-edge-inspection-runtime
/lib/systemd/system/linux-edge-inspection-runtime.service
```

Capture Request Listenerパッケージには主に以下が含まれます。

```text
/opt/linux-edge-inspection-capture-request-listener/
/usr/bin/linux-edge-inspection-capture-request-listener
/lib/systemd/system/linux-edge-inspection-capture-request-listener.service
```

Inspection Workerパッケージには主に以下が含まれます。

```text
/opt/linux-edge-inspection-inspection-worker/
/usr/bin/linux-edge-inspection-inspection-worker
/lib/systemd/system/linux-edge-inspection-inspection-worker.service
```

ライセンス関連文書は各パッケージの `/usr/share/doc/` 配下へ配置します。

---

## ■ SBOM

GitHub Actionsによるリリース時に、  
Microsoft `sbom-tool` を使用してSBOMを生成します。

SBOMは、実際にpublishされた成果物を対象として生成します。

対象：

```text
LinuxEdgeInspection.Runtime
LinuxEdgeInspection.CaptureRequestListener
LinuxEdgeInspection.InspectionWorker
```

Preprocessor / AnalyzerはInspectionWorkerのpublish成果物に含まれます。

ローカル環境では以下のPowerShellスクリプトからSBOMを生成できます。

```powershell
pwsh -ExecutionPolicy Bypass `
  -File .\scripts\sbom\generate-sbom.ps1
```

生成物は `artifacts/` 配下へ出力されます。

### SBOM and OSS License Information

GitHub Actionsでは、debパッケージ作成時にSBOMとOSSライセンス情報も生成します。

生成した情報は、最終的に各debパッケージへ含めます。

#### Microsoft sbom-tool

SPDX形式のSBOM生成に使用します。

- Repository: `microsoft/sbom-tool`
- License: MIT

#### ONOT

生成したSBOMをもとに、  
OSSライセンス情報をまとめた `THIRD-PARTY-NOTICES.md` の生成に使用します。

- Version: 1.1.2
- License: Apache License 2.0

生成処理の流れは以下です。

```text
dotnet publish
      ↓
Microsoft sbom-tool
      ↓
SPDX SBOM
      ↓
ONOT
      ↓
THIRD-PARTY-NOTICES.md
      ↓
Debian Package
```

各debパッケージには、以下のファイルを `/usr/share/doc/<package-name>/` 配下へ格納します。

```text
LICENSE
THIRD-PARTY-NOTICES.md
sbom.spdx.json
```

---

## ■ License

Linux Edge Inspectionは Apache License 2.0 のもとで公開しています。

詳細は以下を参照してください。

```text
LICENSE
```

本ソフトウェアでは.NETのコンポーネントを使用しています。

.NETおよび.NETが利用するThird Party Softwareについては、  
各debパッケージに同梱するSBOMおよびOSSライセンス情報を参照してください。

---

## ■ 現在の対象範囲

現在は、以下を主な対象としています。

- Linux上でのUSBカメラ制御
- 1要求ごとの撮影処理
- Capture RequestのFIFO処理
- systemdを利用したRuntime実行
- InspectionWorkerによる最小Pipeline制御
- Capture → Preprocess → Analyze の順次実行
- Dummy Preprocessorによる前処理Interface検証
- Dummy Analyzerによる分析Interface検証
- Unix Domain SocketによるCapture IPC
- Debian Packageによる配布
- Unit Test
- SBOM生成
- OSSライセンス情報の同梱

以下の機能は今後の検討・実装対象です。

- Equipment Gateway
- 設備 / PLCとの実通信
- 正式なManual / Auto Operation Mode
- 1 Inspectionにおける複数回Capture
- 動的Capture Plan
- 実画像前処理
- 実AI / Rule-based Analysis
- Inspection State管理
- Inspection Recovery
- Management UI
- Preprocessor / Analyzerの独立Process化
- Preprocess / Analysis IPC

---

## ■ Project Policy

本Repositoryでは、  
特定の設備やPLCに依存しない形でLinux上のハードウェア制御と検査処理を整理することを目的としています。

設備固有の制御や実際のAIモデルについては、  
共通基盤から分離する方針です。

現在は、

```text
Request
  ↓
Capture
  ↓
Preprocess
  ↓
Analyze
  ↓
Result
```

という検査処理を構成する各責務を分離し、  
段階的に実装・検証しています。

将来は、Equipment GatewayやManagement等を追加できる構造を維持しつつ、  
現在の最小PoCではLinux上でのCapture Pipeline成立確認を優先します。
