# Linux Edge Inspection

Linux環境上でカメラ撮影や検査処理を実行するための、
エッジ向け検査プラットフォームのOSSサンプルです。

本Repositoryでは、設備やセンサーからの撮影要求を受け取り、
Linux上のUSBカメラを使用して画像を取得するための
基本的な実行基盤を検証・実装しています。

> [!NOTE]
> 本プロジェクトは現在開発中です。
> v0.1.0では、カメラ撮影RuntimeとCapture Request Listenerを中心とした
> 基本的な実行環境を提供します。

---

## ■ 現在の構成

v0.1.0では、主に以下のコンポーネントで構成されています。

```text
Capture Request
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
```

### LinuxEdgeInspection.Runtime

1回の起動につき1回のカメラ撮影を実行する、
one-shot形式のコンソールアプリケーションです。

主な役割：

- カメラ利用環境の確認
- V4L2を使用したUSBカメラ撮影
- 撮影画像の保存
- 撮影結果の出力

### LinuxEdgeInspection.CaptureRequestListener

撮影要求を受け付け、FIFO順にRuntimeを起動する
Worker Serviceです。

主な役割：

- Capture Requestの受付
- Capture Requestのキューイング
- 要求の逐次処理
- systemd経由でRuntimeを実行
- Runtime実行結果の取得

Development環境では、
実機や外部システムがなくても動作確認できるように
Fake Capture Request Generatorを利用できます。

---

## ■ Solution構成

```text
mono-linux-edge-inspection/
├─ src/
│  ├─ Camera/
│  │  ├─ LinuxEdgeInspection.Camera.Abstractions/
│  │  └─ LinuxEdgeInspection.Camera.V4L2/
│  │
│  ├─ LinuxEdgeInspection.Runtime/
│  └─ LinuxEdgeInspection.CaptureRequestListener/
│
├─ tests/
│  ├─ Camera/
│  │  └─ LinuxEdgeInspection.Camera.V4L2.Tests/
│  │
│  ├─ LinuxEdgeInspection.Runtime.Tests/
│  └─ LinuxEdgeInspection.CaptureRequestListener.Tests/
│
├─ packaging/
│  ├─ runtime-deb/
│  └─ capture-request-listener-deb/
│
├─ scripts/
│  └─ sbom/
│
├─ licenses/
│  └─ dotnet/
│
├─ LinuxEdgeInspection.sln
├─ LICENSE
├─ THIRD-PARTY-NOTICES.md
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

GitHub Actionsでは `linux-x64` 向けに
framework-dependent形式でpublishします。

そのため、実行環境には.NET 10 Runtimeが必要です。

---

## ■ Build

Repositoryルートで以下を実行します。

```bash
dotnet restore
dotnet build --configuration Release
```

---

## ■ Test

Solution全体のUnit Testは以下で実行できます。

```bash
dotnet test --configuration Release
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

Development環境では、
Fake Capture Request Generatorを使用して
Capture Requestを生成できます。

Production環境ではFake Generatorは使用しません。

---

## ■ Debian Package

GitHub Actionsでは以下のDebian Packageを生成します。

```text
linux-edge-inspection-runtime_<version>_amd64.deb

linux-edge-inspection-capture-request-listener_<version>_amd64.deb
```

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

ライセンス関連文書は各パッケージの
`/usr/share/doc/` 配下へ配置します。

---

## ■ SBOM

GitHub Actionsによるリリース時に、
Microsoft `sbom-tool` を使用してSBOMを生成します。

SBOMは、実際にpublishされた成果物を対象として生成します。

対象：

```text
LinuxEdgeInspection.Runtime
LinuxEdgeInspection.CaptureRequestListener
```

ローカル環境では以下のPowerShellスクリプトから
SBOMを生成できます。

```powershell
pwsh -ExecutionPolicy Bypass `
  -File .\scripts\sbom\generate-sbom.ps1
```

生成物は `artifacts/` 配下へ出力されます。

---

## ■ License

Linux Edge Inspectionは
**Apache License 2.0** のもとで公開しています。

詳細は以下を参照してください。

```text
LICENSE
```

本ソフトウェアでは.NETのコンポーネントを使用しています。

.NETおよび.NETが利用するThird Party Softwareについては、
以下を参照してください。

```text
THIRD-PARTY-NOTICES.md

licenses/dotnet/LICENSE.txt
licenses/dotnet/THIRD-PARTY-NOTICES.txt
```

---

## ■ 現在の対象範囲

v0.1.0では、以下を主な対象としています。

- Linux上でのUSBカメラ制御
- 1要求ごとの撮影処理
- Capture RequestのFIFO処理
- systemdを利用したRuntime実行
- Debian Packageによる配布
- Unit Test
- SBOM生成
- OSSライセンス情報の同梱

以下の機能は今後の検討・実装対象です。

- Inspection Worker
- Equipment Gateway
- 設備 / PLCとの実通信
- Manual / Auto Operation Mode
- 1検査における複数回撮影
- 画像前処理
- AI / Rule-based Analysis
- 管理UI

---

## ■ Project Policy

本Repositoryでは、
特定の設備やPLCに依存しない形で
Linux上のハードウェア制御と検査処理を整理することを目的としています。

設備固有の制御や実際のAIモデルについては、
共通基盤から分離する方針です。

まずは、

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
段階的に実装・検証していきます。
