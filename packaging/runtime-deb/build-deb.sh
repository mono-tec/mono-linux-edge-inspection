#!/usr/bin/env bash

# エラー発生時に処理を停止します。
# -e : コマンドが失敗した場合に終了
# -u : 未定義変数を使用した場合に終了
# -o pipefail : パイプ途中のコマンド失敗も検出
set -euo pipefail

# ------------------------------------------------------------
# パッケージ基本情報
# ------------------------------------------------------------

# Debianパッケージ名とsystemdサービス名に使用します。
PACKAGE_NAME="linux-edge-inspection-runtime"

# 環境変数が指定されていない場合は0.1.0を使用します。
PACKAGE_VERSION="${PACKAGE_VERSION:-0.1.0}"

# Debianパッケージの対象CPUアーキテクチャです。
PACKAGE_ARCHITECTURE="${PACKAGE_ARCHITECTURE:-amd64}"

# dotnet publishで使用するRuntime Identifierです。
RUNTIME_IDENTIFIER="${RUNTIME_IDENTIFIER:-linux-x64}"

# ------------------------------------------------------------
# パス設定
# ------------------------------------------------------------

# このスクリプトが配置されているディレクトリを取得します。
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# packaging/runtime-debから2階層上をRepositoryルートとします。
REPOSITORY_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# publish対象となる撮影Runtimeプロジェクトです。
PROJECT_PATH="${REPOSITORY_ROOT}/src/LinuxEdgeInspection.Runtime/LinuxEdgeInspection.Runtime.csproj"

# publish後に実行するアプリケーションDLLです。
APPLICATION_DLL="LinuxEdgeInspection.Runtime.dll"

# パッケージへ含めるsystemd Unitファイルです。
SERVICE_FILE="${PACKAGE_NAME}.service"

# Debianパッケージ構築用の一時作業ディレクトリです。
WORK_DIR="${REPOSITORY_ROOT}/artifacts/deb/${PACKAGE_NAME}_${PACKAGE_VERSION}_${PACKAGE_ARCHITECTURE}"

# 完成したdebパッケージの出力先です。
OUTPUT_DIR="${REPOSITORY_ROOT}/artifacts/packages"

# dotnet publishの出力先です。
# GitHub Actions側からPUBLISH_DIRを指定することもできます。
PUBLISH_DIR="${PUBLISH_DIR:-${REPOSITORY_ROOT}/artifacts/publish/${PACKAGE_NAME}}"

# Debianパッケージ内のドキュメント配置先です。
DOCUMENT_DIR="${WORK_DIR}/usr/share/doc/${PACKAGE_NAME}"

# ------------------------------------------------------------
# Debianパッケージ用ディレクトリの作成
# ------------------------------------------------------------

# 以前の作業ディレクトリが残っている場合は削除します。
rm -rf "${WORK_DIR}"

# Debianパッケージ内で必要になるディレクトリを作成します。
mkdir -p \
  "${WORK_DIR}/DEBIAN" \
  "${WORK_DIR}/opt/${PACKAGE_NAME}" \
  "${WORK_DIR}/usr/bin" \
  "${WORK_DIR}/lib/systemd/system" \
  "${DOCUMENT_DIR}/licenses/dotnet" \
  "${OUTPUT_DIR}"

# ------------------------------------------------------------
# .NETアプリケーションのpublish
# ------------------------------------------------------------

# 指定されたpublishディレクトリにDLLが存在しない場合のみ、
# dotnet publishを実行します。
#
# GitHub Actionsで事前にpublish済みの場合は、
# PUBLISH_DIRを指定することで再publishを省略できます。
if [[ ! -f "${PUBLISH_DIR}/${APPLICATION_DLL}" ]]; then
  rm -rf "${PUBLISH_DIR}"

  dotnet publish "${PROJECT_PATH}" \
    --configuration Release \
    --runtime "${RUNTIME_IDENTIFIER}" \
    --self-contained false \
    --output "${PUBLISH_DIR}" \
    -p:Version="${PACKAGE_VERSION}" \
    -p:ContinuousIntegrationBuild=true
fi

# ------------------------------------------------------------
# publish成果物とsystemd Unitファイルの配置
# ------------------------------------------------------------

# publish成果物を/opt配下へ配置します。
cp -a \
  "${PUBLISH_DIR}/." \
  "${WORK_DIR}/opt/${PACKAGE_NAME}/"

# systemd Unitファイルをパッケージへ配置します。
cp \
  "${SCRIPT_DIR}/${SERVICE_FILE}" \
  "${WORK_DIR}/lib/systemd/system/${SERVICE_FILE}"

# ------------------------------------------------------------
# ライセンス文書の配置
# ------------------------------------------------------------

# Linux Edge Inspection本体のApache License 2.0を配置します。
cp \
  "${REPOSITORY_ROOT}/LICENSE" \
  "${DOCUMENT_DIR}/LICENSE"

# Linux Edge Inspection側のThird Party案内文書を配置します。
cp \
  "${REPOSITORY_ROOT}/THIRD-PARTY-NOTICES.md" \
  "${DOCUMENT_DIR}/THIRD-PARTY-NOTICES.md"

# .NET公式のMIT Licenseを配置します。
cp \
  "${REPOSITORY_ROOT}/licenses/dotnet/LICENSE.TXT" \
  "${DOCUMENT_DIR}/licenses/dotnet/LICENSE.TXT"

# .NET公式のThird Party Noticesを配置します。
cp \
  "${REPOSITORY_ROOT}/licenses/dotnet/THIRD-PARTY-NOTICES.TXT" \
  "${DOCUMENT_DIR}/licenses/dotnet/THIRD-PARTY-NOTICES.TXT"

# ------------------------------------------------------------
# 実行用ラッパースクリプトの作成
# ------------------------------------------------------------

# /usr/bin/linux-edge-inspection-runtimeを作成します。
#
# 利用者やsystemdからは、このコマンドを呼び出すことで
# /opt配下の.NETアプリケーションを実行できます。
cat > "${WORK_DIR}/usr/bin/${PACKAGE_NAME}" <<'WRAPPER'
#!/usr/bin/env bash

exec /usr/bin/dotnet \
  /opt/linux-edge-inspection-runtime/LinuxEdgeInspection.Runtime.dll \
  "$@"
WRAPPER

# 実行用ラッパーへ実行権限を付与します。
chmod 0755 "${WORK_DIR}/usr/bin/${PACKAGE_NAME}"

# ------------------------------------------------------------
# Debian controlファイルの作成
# ------------------------------------------------------------

# パッケージ名、バージョン、対象アーキテクチャ、
# 依存パッケージなどを定義します。
#
# 本Runtimeは.NET 10 Runtimeに加えて、
# V4L2カメラ操作用のv4l-utilsを必要とします。
cat > "${WORK_DIR}/DEBIAN/control" <<CONTROL
Package: ${PACKAGE_NAME}
Version: ${PACKAGE_VERSION}
Section: utils
Priority: optional
Architecture: ${PACKAGE_ARCHITECTURE}
Depends: dotnet-runtime-10.0, v4l-utils
Maintainer: mono-tec
Description: Camera capture runtime for Linux Edge Inspection
 A framework-dependent .NET application that captures one image from a
 V4L2 USB camera and then exits.
CONTROL

# ------------------------------------------------------------
# インストール後処理の作成
# ------------------------------------------------------------

# パッケージのインストール後にsystemdのUnit定義を再読み込みします。
#
# サービスの自動有効化や自動起動は行わず、
# 実機検証時に明示的に操作できるようにしています。
cat > "${WORK_DIR}/DEBIAN/postinst" <<'POSTINST'
#!/usr/bin/env bash
set -e

systemctl daemon-reload || true

exit 0
POSTINST

chmod 0755 "${WORK_DIR}/DEBIAN/postinst"

# ------------------------------------------------------------
# アンインストール前処理の作成
# ------------------------------------------------------------

# パッケージ削除前にサービスを停止し、
# 自動起動設定も解除します。
#
# サービスが未登録または未起動でも、
# アンインストール処理を継続できるようにしています。
cat > "${WORK_DIR}/DEBIAN/prerm" <<'PRERM'
#!/usr/bin/env bash
set -e

systemctl stop \
  linux-edge-inspection-runtime.service \
  2>/dev/null || true

systemctl disable \
  linux-edge-inspection-runtime.service \
  2>/dev/null || true

exit 0
PRERM

chmod 0755 "${WORK_DIR}/DEBIAN/prerm"

# ------------------------------------------------------------
# アンインストール後処理の作成
# ------------------------------------------------------------

# Unitファイル削除後にsystemdの定義を再読み込みします。
cat > "${WORK_DIR}/DEBIAN/postrm" <<'POSTRM'
#!/usr/bin/env bash
set -e

systemctl daemon-reload || true

exit 0
POSTRM

chmod 0755 "${WORK_DIR}/DEBIAN/postrm"

# ------------------------------------------------------------
# Debianパッケージの作成
# ------------------------------------------------------------

# 完成するdebファイルのパスです。
PACKAGE_PATH="${OUTPUT_DIR}/${PACKAGE_NAME}_${PACKAGE_VERSION}_${PACKAGE_ARCHITECTURE}.deb"

# 作業ディレクトリからdebパッケージを作成します。
#
# --root-owner-groupを指定することで、
# パッケージ内ファイルの所有者をroot:rootとして扱います。
dpkg-deb \
  --build \
  --root-owner-group \
  "${WORK_DIR}" \
  "${PACKAGE_PATH}"

# GitHub Actionsや手動実行時に、
# 作成されたファイルの場所を確認できるように表示します。
echo "Created: ${PACKAGE_PATH}"