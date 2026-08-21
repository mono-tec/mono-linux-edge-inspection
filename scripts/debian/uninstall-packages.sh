#!/usr/bin/env bash

# Linux Edge InspectionのDebian Packageをまとめてアンインストールします。
#
# 対象パッケージ:
# - linux-edge-inspection-image-cleanup
# - linux-edge-inspection-management
# - linux-edge-inspection-management-api
# - linux-edge-inspection-inspection-worker
# - linux-edge-inspection-capture-request-listener
# - linux-edge-inspection-runtime
#
# アンインストール順は依存関係や利用関係を考慮し、
# Image Cleanup → Management → Management API
# → Inspection Worker → Capture Request Listener → Runtime
# とします。
#
# Image Cleanupはsystemd timerから起動されるため、
# パッケージ削除前にtimerを停止・無効化します。
#
# 本スクリプトでは完全アンインストールとして、
# 以下のLinux Edge Inspection専用リソースも削除します。
#
# - Unix Domain Socket用Runtime Directory
# - 撮像画像・Runtime状態ファイル
# - Runtime再起動用sudoers設定
# - Linux Edge Inspection専用ユーザー
# - Linux Edge Inspection専用グループ

set -euo pipefail

# ------------------------------------------------------------
# パッケージ名
# ------------------------------------------------------------

IMAGE_CLEANUP_PACKAGE="linux-edge-inspection-image-cleanup"
MANAGEMENT_PACKAGE="linux-edge-inspection-management"
MANAGEMENT_API_PACKAGE="linux-edge-inspection-management-api"
WORKER_PACKAGE="linux-edge-inspection-inspection-worker"
LISTENER_PACKAGE="linux-edge-inspection-capture-request-listener"
RUNTIME_PACKAGE="linux-edge-inspection-runtime"

# ------------------------------------------------------------
# 共通リソース
# ------------------------------------------------------------

# Linux Edge Inspection専用実行ユーザーです。
SERVICE_USER="linux-edge-inspection"

# Linux Edge Inspection専用グループです。
SERVICE_GROUP="linux-edge-inspection"

# Unix Domain Socketを配置するRuntime Directoryです。
RUNTIME_DIRECTORY="/run/linux-edge-inspection"

# 撮像画像やRuntime状態ファイルを保存するData Directoryです。
DATA_DIRECTORY="/var/lib/linux-edge-inspection"

# Capture Request ListenerからRuntimeを再起動するための
# sudoers設定ファイルです。
SUDOERS_FILE="/etc/sudoers.d/linux-edge-inspection-runtime"

# ------------------------------------------------------------
# Image Cleanup Timerの停止
# ------------------------------------------------------------

echo "Stopping Linux Edge Inspection Image Cleanup timer..."

# Image Cleanupの定期実行を停止します。
#
# アンインストール処理中にtimerからImage Cleanupが
# 新しく起動されないよう、他のサービスより先に停止します。
sudo systemctl stop \
  linux-edge-inspection-image-cleanup.timer \
  2>/dev/null || true

# 次回起動時にもtimerが自動有効化されないようにします。
sudo systemctl disable \
  linux-edge-inspection-image-cleanup.timer \
  2>/dev/null || true

# Image Cleanupが実行中の場合は停止します。
sudo systemctl stop \
  linux-edge-inspection-image-cleanup.service \
  2>/dev/null || true

# ------------------------------------------------------------
# サービス停止
# ------------------------------------------------------------

echo
echo "Stopping Linux Edge Inspection services..."

# Management UIを停止します。
sudo systemctl stop \
  linux-edge-inspection-management.service \
  2>/dev/null || true

# Management APIを停止します。
sudo systemctl stop \
  linux-edge-inspection-management-api.service \
  2>/dev/null || true

# Inspection Workerを停止します。
sudo systemctl stop \
  linux-edge-inspection-inspection-worker.service \
  2>/dev/null || true

# Capture Request Listenerを停止します。
sudo systemctl stop \
  linux-edge-inspection-capture-request-listener.service \
  2>/dev/null || true

# Runtimeを停止します。
sudo systemctl stop \
  linux-edge-inspection-runtime.service \
  2>/dev/null || true

# ------------------------------------------------------------
# Debian Packageのアンインストール
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection packages..."

# Image Cleanupを含む6パッケージを削除します。
#
# Image Cleanupは撮像データを利用する側なので先に削除し、
# その後、上位側のManagement / Management API / Workerから
# Listener / Runtimeの順で削除します。
#
# 各パッケージのprerm / postrmでも、
# Service / Timer停止やSocket残骸の削除などを実行します。
sudo apt-get remove -y \
  "${IMAGE_CLEANUP_PACKAGE}" \
  "${MANAGEMENT_PACKAGE}" \
  "${MANAGEMENT_API_PACKAGE}" \
  "${WORKER_PACKAGE}" \
  "${LISTENER_PACKAGE}" \
  "${RUNTIME_PACKAGE}"

# ------------------------------------------------------------
# sudoers設定の削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection sudoers configuration..."

# Capture Request Listener用に追加した、
# Runtime再起動専用sudoers設定が残っている場合は削除します。
sudo rm -f \
  "${SUDOERS_FILE}"

# ------------------------------------------------------------
# Runtime Directoryの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection runtime directory..."

# Unix Domain Socketなどの一時Runtimeデータを削除します。
sudo rm -rf \
  "${RUNTIME_DIRECTORY}"

# ------------------------------------------------------------
# Data Directoryの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection data directory..."

# 撮像画像やcapture-result.jsonなど、
# Linux Edge Inspection専用データを削除します。
#
# Image Cleanupパッケージ単体のアンインストールでは
# 撮像画像を削除しませんが、
# 本スクリプトは製品全体の完全アンインストールを目的とするため、
# 共通Data Directoryをまとめて削除します。
sudo rm -rf \
  "${DATA_DIRECTORY}"

# ------------------------------------------------------------
# 専用ユーザーの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection service user..."

# Linux Edge Inspection共通実行ユーザーを削除します。
#
# videoやsystemd-journalなどの補助グループへの所属も、
# ユーザー削除に伴って不要になります。
sudo userdel \
  "${SERVICE_USER}" \
  2>/dev/null || true

# ------------------------------------------------------------
# 専用グループの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection service group..."

# Linux Edge Inspection共通グループを削除します。
sudo groupdel \
  "${SERVICE_GROUP}" \
  2>/dev/null || true

# ------------------------------------------------------------
# systemd定義の再読み込み
# ------------------------------------------------------------

# Package削除後のService / Timer定義をsystemdへ反映します。
sudo systemctl daemon-reload

# ------------------------------------------------------------
# アンインストール結果の確認
# ------------------------------------------------------------

echo
echo "Remaining Linux Edge Inspection packages:"

dpkg -l \
  | grep 'linux-edge-inspection' \
  || true

echo
echo "Remaining Linux Edge Inspection resources:"

if [[ -e "${RUNTIME_DIRECTORY}" ]]; then
  echo "  Remaining: ${RUNTIME_DIRECTORY}"
fi

if [[ -e "${DATA_DIRECTORY}" ]]; then
  echo "  Remaining: ${DATA_DIRECTORY}"
fi

if [[ -e "${SUDOERS_FILE}" ]]; then
  echo "  Remaining: ${SUDOERS_FILE}"
fi

if id "${SERVICE_USER}" >/dev/null 2>&1; then
  echo "  Remaining user: ${SERVICE_USER}"
fi

if getent group "${SERVICE_GROUP}" >/dev/null 2>&1; then
  echo "  Remaining group: ${SERVICE_GROUP}"
fi

# ------------------------------------------------------------
# Image Cleanup Timer残存確認
# ------------------------------------------------------------

# Unitファイルやenableリンクが残っていないか確認します。
if systemctl list-unit-files \
  | grep -q '^linux-edge-inspection-image-cleanup\.timer'; then
  echo "  Remaining timer: linux-edge-inspection-image-cleanup.timer"
fi

if systemctl list-unit-files \
  | grep -q '^linux-edge-inspection-image-cleanup\.service'; then
  echo "  Remaining service: linux-edge-inspection-image-cleanup.service"
fi

# ------------------------------------------------------------
# 完了メッセージ
# ------------------------------------------------------------

echo
echo "Uninstallation completed."

echo
echo "Removed resources:"
echo "  Packages          : 6 Linux Edge Inspection packages"
echo "  Runtime directory : ${RUNTIME_DIRECTORY}"
echo "  Data directory    : ${DATA_DIRECTORY}"
echo "  Sudoers           : ${SUDOERS_FILE}"
echo "  Service user      : ${SERVICE_USER}"
echo "  Service group     : ${SERVICE_GROUP}"