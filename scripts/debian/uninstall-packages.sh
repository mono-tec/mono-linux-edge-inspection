#!/usr/bin/env bash

# Linux Edge Inspection の Debian Package をまとめてアンインストールします。
#
# amd64 / arm64 共通で使用できます。
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
# Image Cleanup は systemd timer から起動されるため、
# パッケージ削除前に timer を停止・無効化します。
#
# 本スクリプトでは完全アンインストールとして、
# 以下の Linux Edge Inspection 専用リソースも削除します。
#
# - Unix Domain Socket 用 Runtime Directory
# - 撮像画像・Runtime 状態ファイル
# - Runtime 再起動用 sudoers 設定
# - Linux Edge Inspection 専用ユーザー
# - Linux Edge Inspection 専用グループ

set -euo pipefail

# ------------------------------------------------------------
# 必要コマンドの確認
# ------------------------------------------------------------

for command_name in apt-get dpkg systemctl id getent userdel groupdel; do
  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "ERROR: Required command not found: ${command_name}"
    exit 1
  fi
done

# ------------------------------------------------------------
# パッケージ名
# ------------------------------------------------------------

IMAGE_CLEANUP_PACKAGE="linux-edge-inspection-image-cleanup"
MANAGEMENT_PACKAGE="linux-edge-inspection-management"
MANAGEMENT_API_PACKAGE="linux-edge-inspection-management-api"
WORKER_PACKAGE="linux-edge-inspection-inspection-worker"
LISTENER_PACKAGE="linux-edge-inspection-capture-request-listener"
RUNTIME_PACKAGE="linux-edge-inspection-runtime"

PACKAGES=(
  "${IMAGE_CLEANUP_PACKAGE}"
  "${MANAGEMENT_PACKAGE}"
  "${MANAGEMENT_API_PACKAGE}"
  "${WORKER_PACKAGE}"
  "${LISTENER_PACKAGE}"
  "${RUNTIME_PACKAGE}"
)

# ------------------------------------------------------------
# 共通リソース
# ------------------------------------------------------------

SERVICE_USER="linux-edge-inspection"
SERVICE_GROUP="linux-edge-inspection"

RUNTIME_DIRECTORY="/run/linux-edge-inspection"
DATA_DIRECTORY="/var/lib/linux-edge-inspection"

SUDOERS_FILE="/etc/sudoers.d/linux-edge-inspection-runtime"

# ------------------------------------------------------------
# 実行環境の表示
# ------------------------------------------------------------

ARCHITECTURE="$(dpkg --print-architecture)"

echo "Linux Edge Inspection uninstaller"
echo "Architecture: ${ARCHITECTURE}"

# ------------------------------------------------------------
# Image Cleanup Timer の停止
# ------------------------------------------------------------

echo
echo "Stopping Linux Edge Inspection Image Cleanup timer..."

sudo systemctl stop \
  linux-edge-inspection-image-cleanup.timer \
  2>/dev/null || true

sudo systemctl disable \
  linux-edge-inspection-image-cleanup.timer \
  2>/dev/null || true

sudo systemctl stop \
  linux-edge-inspection-image-cleanup.service \
  2>/dev/null || true

# ------------------------------------------------------------
# サービス停止
# ------------------------------------------------------------

echo
echo "Stopping Linux Edge Inspection services..."

sudo systemctl stop \
  linux-edge-inspection-management.service \
  2>/dev/null || true

sudo systemctl stop \
  linux-edge-inspection-management-api.service \
  2>/dev/null || true

sudo systemctl stop \
  linux-edge-inspection-inspection-worker.service \
  2>/dev/null || true

sudo systemctl stop \
  linux-edge-inspection-capture-request-listener.service \
  2>/dev/null || true

sudo systemctl stop \
  linux-edge-inspection-runtime.service \
  2>/dev/null || true

# ------------------------------------------------------------
# インストール済みパッケージの確認
# ------------------------------------------------------------

echo
echo "Checking installed Linux Edge Inspection packages..."

INSTALLED_PACKAGES=()

for package_name in "${PACKAGES[@]}"; do
  if dpkg-query -W \
    -f='${db:Status-Status}' \
    "${package_name}" \
    2>/dev/null \
    | grep -qx 'installed'; then

    echo "  Installed: ${package_name}"
    INSTALLED_PACKAGES+=("${package_name}")
  else
    echo "  Not installed: ${package_name}"
  fi
done

# ------------------------------------------------------------
# Debian Package のアンインストール
# ------------------------------------------------------------

if [[ "${#INSTALLED_PACKAGES[@]}" -gt 0 ]]; then
  echo
  echo "Removing Linux Edge Inspection packages..."

  # PACKAGES 配列の順序で確認しているため、
  # Image Cleanup → Management → Management API
  # → Inspection Worker → Capture Request Listener → Runtime
  # の順で apt-get へ渡します。
  sudo apt-get remove -y "${INSTALLED_PACKAGES[@]}"
else
  echo
  echo "No Linux Edge Inspection packages are currently installed."
fi

# ------------------------------------------------------------
# sudoers 設定の削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection sudoers configuration..."

sudo rm -f \
  "${SUDOERS_FILE}"

# ------------------------------------------------------------
# Runtime Directory の削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection runtime directory..."

sudo rm -rf \
  "${RUNTIME_DIRECTORY}"

# ------------------------------------------------------------
# Data Directory の削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection data directory..."

# 本スクリプトは製品全体の完全アンインストールを目的とするため、
# 撮像画像や Runtime 状態ファイルを含む共通 Data Directory も削除します。
sudo rm -rf \
  "${DATA_DIRECTORY}"

# ------------------------------------------------------------
# 専用ユーザーの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection service user..."

if id "${SERVICE_USER}" >/dev/null 2>&1; then
  sudo userdel "${SERVICE_USER}"
else
  echo "  User does not exist: ${SERVICE_USER}"
fi

# ------------------------------------------------------------
# 専用グループの削除
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection service group..."

if getent group "${SERVICE_GROUP}" >/dev/null 2>&1; then
  sudo groupdel "${SERVICE_GROUP}"
else
  echo "  Group does not exist: ${SERVICE_GROUP}"
fi

# ------------------------------------------------------------
# systemd 定義の再読み込み
# ------------------------------------------------------------

sudo systemctl daemon-reload
sudo systemctl reset-failed 2>/dev/null || true

# ------------------------------------------------------------
# アンインストール結果の確認
# ------------------------------------------------------------

echo
echo "Remaining Linux Edge Inspection packages:"

REMAINING_PACKAGES="$(
  dpkg -l \
    | grep 'linux-edge-inspection' \
    || true
)"

if [[ -n "${REMAINING_PACKAGES}" ]]; then
  echo "${REMAINING_PACKAGES}"
else
  echo "  None"
fi

echo
echo "Remaining Linux Edge Inspection resources:"

REMAINING_RESOURCE_FOUND=false

if [[ -e "${RUNTIME_DIRECTORY}" ]]; then
  echo "  Remaining: ${RUNTIME_DIRECTORY}"
  REMAINING_RESOURCE_FOUND=true
fi

if [[ -e "${DATA_DIRECTORY}" ]]; then
  echo "  Remaining: ${DATA_DIRECTORY}"
  REMAINING_RESOURCE_FOUND=true
fi

if [[ -e "${SUDOERS_FILE}" ]]; then
  echo "  Remaining: ${SUDOERS_FILE}"
  REMAINING_RESOURCE_FOUND=true
fi

if id "${SERVICE_USER}" >/dev/null 2>&1; then
  echo "  Remaining user: ${SERVICE_USER}"
  REMAINING_RESOURCE_FOUND=true
fi

if getent group "${SERVICE_GROUP}" >/dev/null 2>&1; then
  echo "  Remaining group: ${SERVICE_GROUP}"
  REMAINING_RESOURCE_FOUND=true
fi

if [[ "${REMAINING_RESOURCE_FOUND}" == false ]]; then
  echo "  None"
fi

# ------------------------------------------------------------
# Image Cleanup Timer / Service 残存確認
# ------------------------------------------------------------

echo
echo "Checking remaining systemd units..."

REMAINING_UNIT_FOUND=false

if systemctl list-unit-files \
  | grep -q '^linux-edge-inspection-image-cleanup\.timer'; then
  echo "  Remaining timer: linux-edge-inspection-image-cleanup.timer"
  REMAINING_UNIT_FOUND=true
fi

if systemctl list-unit-files \
  | grep -q '^linux-edge-inspection-image-cleanup\.service'; then
  echo "  Remaining service: linux-edge-inspection-image-cleanup.service"
  REMAINING_UNIT_FOUND=true
fi

if [[ "${REMAINING_UNIT_FOUND}" == false ]]; then
  echo "  None"
fi

# ------------------------------------------------------------
# 完了メッセージ
# ------------------------------------------------------------

echo
echo "Uninstallation completed."

echo
echo "Target architecture : ${ARCHITECTURE}"
echo "Removed resources:"
echo "  Packages          : Linux Edge Inspection packages"
echo "  Runtime directory : ${RUNTIME_DIRECTORY}"
echo "  Data directory    : ${DATA_DIRECTORY}"
echo "  Sudoers           : ${SUDOERS_FILE}"
echo "  Service user      : ${SERVICE_USER}"
echo "  Service group     : ${SERVICE_GROUP}"
