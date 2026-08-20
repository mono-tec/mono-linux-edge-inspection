#!/usr/bin/env bash

# Linux Edge InspectionのDebian Packageをまとめてアンインストールします。
#
# 対象パッケージ:
# - linux-edge-inspection-management
# - linux-edge-inspection-management-api
# - linux-edge-inspection-inspection-worker
# - linux-edge-inspection-capture-request-listener
# - linux-edge-inspection-runtime
#
# アンインストール順は依存関係や利用関係を考慮し、
# Management → Management API → Inspection Worker
# → Capture Request Listener → Runtime
# とします。
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
# サービス停止
# ------------------------------------------------------------

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

# 上位側のManagement / Management API / Workerから順に指定します。
#
# 各パッケージのprerm / postrmでも、
# サービス停止やSocket残骸の削除などを実行します。
sudo apt-get remove -y \
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
# 本システムでは判定用画像を保持対象としないため、
# 完全アンインストール時にまとめて削除します。
sudo rm -rf \
  "${DATA_DIRECTORY}"

# ------------------------------------------------------------
# 専用ユーザーの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection service user..."

# 専用ユーザーを削除します。
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

sudo groupdel \
  "${SERVICE_GROUP}" \
  2>/dev/null || true

# ------------------------------------------------------------
# systemd定義の再読み込み
# ------------------------------------------------------------

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
# 完了メッセージ
# ------------------------------------------------------------

echo
echo "Uninstallation completed."

echo
echo "Removed resources:"
echo "  Packages"
echo "  Runtime directory : ${RUNTIME_DIRECTORY}"
echo "  Data directory    : ${DATA_DIRECTORY}"
echo "  Sudoers           : ${SUDOERS_FILE}"
echo "  Service user      : ${SERVICE_USER}"
echo "  Service group     : ${SERVICE_GROUP}"