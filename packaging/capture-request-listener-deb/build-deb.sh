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
PACKAGE_NAME="linux-edge-inspection-capture-request-listener"

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

# packaging/capture-request-listener-debから2階層上をRepositoryルートとします。
REPOSITORY_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# publish対象となるCaptureRequestListenerプロジェクトです。
PROJECT_PATH="${REPOSITORY_ROOT}/src/LinuxEdgeInspection.CaptureRequestListener/LinuxEdgeInspection.CaptureRequestListener.csproj"

# publish後に実行するアプリケーションDLLです。
APPLICATION_DLL="LinuxEdgeInspection.CaptureRequestListener.dll"

# パッケージへ含めるsystemd Unitファイルです。
SERVICE_FILE="${PACKAGE_NAME}.service"

# Debianパッケージ構築用の一時作業ディレクトリです。
WORK_DIR="${REPOSITORY_ROOT}/artifacts/deb/${PACKAGE_NAME}_${PACKAGE_VERSION}_${PACKAGE_ARCHITECTURE}"

# 完成したdebパッケージの出力先です。
OUTPUT_DIR="${REPOSITORY_ROOT}/artifacts/packages"

# dotnet publishの出力先です。
# GitHub Actions側からPUBLISH_DIRを指定することもできます。
PUBLISH_DIR="${PUBLISH_DIR:-${REPOSITORY_ROOT}/artifacts/publish/${PACKAGE_NAME}}"

# GitHub Actionsなどで生成した
# LICENSE / THIRD-PARTY-NOTICES.md / sbom.spdx.json の配置先です。
COMPLIANCE_DIR="${COMPLIANCE_DIR:-${REPOSITORY_ROOT}/artifacts/compliance/${PACKAGE_NAME}}"

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
    "${DOCUMENT_DIR}" \
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
# ライセンス・Notice・SBOMの配置
# ------------------------------------------------------------

# GitHub Actionsなどで生成したCompliance一式が存在することを確認します。
for compliance_file in \
    LICENSE \
    THIRD-PARTY-NOTICES.md \
    sbom.spdx.json
do
    if [[ ! -f "${COMPLIANCE_DIR}/${compliance_file}" ]]; then
        echo "Required compliance file not found: ${COMPLIANCE_DIR}/${compliance_file}"
        exit 1
    fi
done

# Linux Edge Inspection本体のApache License 2.0を配置します。
cp \
    "${COMPLIANCE_DIR}/LICENSE" \
    "${DOCUMENT_DIR}/LICENSE"

# SBOMから自動生成したThird Party Noticeを配置します。
cp \
    "${COMPLIANCE_DIR}/THIRD-PARTY-NOTICES.md" \
    "${DOCUMENT_DIR}/THIRD-PARTY-NOTICES.md"

# SPDX形式のSBOMを配置します。
cp \
    "${COMPLIANCE_DIR}/sbom.spdx.json" \
    "${DOCUMENT_DIR}/sbom.spdx.json"

# ------------------------------------------------------------
# 実行用ラッパースクリプトの作成
# ------------------------------------------------------------

# /usr/bin/linux-edge-inspection-capture-request-listenerを作成します。
#
# 利用者やsystemdからは、このコマンドを呼び出すことで
# /opt配下の.NETアプリケーションを実行できます。
cat > "${WORK_DIR}/usr/bin/${PACKAGE_NAME}" <<'WRAPPER'
#!/usr/bin/env bash

exec /usr/bin/dotnet \
    /opt/linux-edge-inspection-capture-request-listener/LinuxEdgeInspection.CaptureRequestListener.dll \
    "$@"
WRAPPER

# 実行用ラッパーへ実行権限を付与します。
chmod 0755 "${WORK_DIR}/usr/bin/${PACKAGE_NAME}"

# ------------------------------------------------------------
# Debian controlファイルの作成
# ------------------------------------------------------------

# パッケージ名、バージョン、依存パッケージなどを定義します。
#
# CaptureRequestListenerはRuntimeのsystemdサービスを起動するため、
# 同一バージョンのRuntimeパッケージへ依存させます。
cat > "${WORK_DIR}/DEBIAN/control" <<CONTROL
Package: ${PACKAGE_NAME}
Version: ${PACKAGE_VERSION}
Section: utils
Priority: optional
Architecture: ${PACKAGE_ARCHITECTURE}
Depends: dotnet-runtime-10.0, linux-edge-inspection-runtime (= ${PACKAGE_VERSION})
Maintainer: mono-tec
Description: Capture request listener for Linux Edge Inspection
 A framework-dependent .NET worker service that receives capture requests,
 queues them in FIFO order, and starts the camera runtime.
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
    linux-edge-inspection-capture-request-listener.service \
    2>/dev/null || true

systemctl disable \
    linux-edge-inspection-capture-request-listener.service \
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