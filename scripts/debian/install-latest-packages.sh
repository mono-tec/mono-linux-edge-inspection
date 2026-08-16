#!/usr/bin/env bash

# Linux Edge Inspectionの最新GitHub Releaseを取得し、
# 4つのDebian Packageをまとめてダウンロード・インストールします。
#
# 対象パッケージ:
# - linux-edge-inspection-runtime
# - linux-edge-inspection-capture-request-listener
# - linux-edge-inspection-inspection-worker
# - linux-edge-inspection-management

set -euo pipefail

# ------------------------------------------------------------
# 基本設定
# ------------------------------------------------------------

REPOSITORY_OWNER="mono-tec"
REPOSITORY_NAME="mono-linux-edge-inspection"
ARCHITECTURE="amd64"

GITHUB_RELEASE_BASE_URL="https://github.com/${REPOSITORY_OWNER}/${REPOSITORY_NAME}/releases"

# ------------------------------------------------------------
# 必要コマンドの確認
# ------------------------------------------------------------

for command_name in curl wget apt-get dpkg; do
  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "Required command not found: ${command_name}"
    exit 1
  fi
done

# ------------------------------------------------------------
# 最新Releaseタグの取得
# ------------------------------------------------------------

echo "Checking latest GitHub Release..."

LATEST_RELEASE_URL="$(
  curl \
    -fsSL \
    -o /dev/null \
    -w '%{url_effective}' \
    "${GITHUB_RELEASE_BASE_URL}/latest"
)"

LATEST_TAG="${LATEST_RELEASE_URL##*/}"

if [[ -z "${LATEST_TAG}" || "${LATEST_TAG}" == "latest" ]]; then
  echo "Failed to determine latest release tag."
  exit 1
fi

# Debian PackageのVersionでは先頭のvを除去します。
PACKAGE_VERSION="${LATEST_TAG#v}"

echo "Latest release: ${LATEST_TAG}"
echo "Package version: ${PACKAGE_VERSION}"

# ------------------------------------------------------------
# 一時作業ディレクトリの作成
# ------------------------------------------------------------

WORK_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "${WORK_DIR}"
}

trap cleanup EXIT

cd "${WORK_DIR}"

echo "Working directory: ${WORK_DIR}"

# ------------------------------------------------------------
# パッケージ名
# ------------------------------------------------------------

RUNTIME_PACKAGE="linux-edge-inspection-runtime_${PACKAGE_VERSION}_${ARCHITECTURE}.deb"

LISTENER_PACKAGE="linux-edge-inspection-capture-request-listener_${PACKAGE_VERSION}_${ARCHITECTURE}.deb"

WORKER_PACKAGE="linux-edge-inspection-inspection-worker_${PACKAGE_VERSION}_${ARCHITECTURE}.deb"

MANAGEMENT_PACKAGE="linux-edge-inspection-management_${PACKAGE_VERSION}_${ARCHITECTURE}.deb"

DOWNLOAD_BASE_URL="${GITHUB_RELEASE_BASE_URL}/download/${LATEST_TAG}"

# ------------------------------------------------------------
# 最新ReleaseからDebian Packageをダウンロード
# ------------------------------------------------------------

download_package() {
  local package_name="$1"

  echo "Downloading: ${package_name}"

  wget \
    --quiet \
    --show-progress \
    "${DOWNLOAD_BASE_URL}/${package_name}"
}

download_package "${RUNTIME_PACKAGE}"
download_package "${LISTENER_PACKAGE}"
download_package "${WORKER_PACKAGE}"
download_package "${MANAGEMENT_PACKAGE}"

echo
echo "Downloaded packages:"

ls -lh \
  "${RUNTIME_PACKAGE}" \
  "${LISTENER_PACKAGE}" \
  "${WORKER_PACKAGE}" \
  "${MANAGEMENT_PACKAGE}"

# ------------------------------------------------------------
# Debian Packageをまとめてインストール
# ------------------------------------------------------------

echo
echo "Installing Linux Edge Inspection packages..."

# 4つを同時にaptへ渡すことで、
# ローカルdeb同士の依存関係と.NET Runtime依存を
# aptにまとめて解決させます。
sudo apt-get update

sudo apt-get install -y \
  "./${RUNTIME_PACKAGE}" \
  "./${LISTENER_PACKAGE}" \
  "./${WORKER_PACKAGE}" \
  "./${MANAGEMENT_PACKAGE}"

# ------------------------------------------------------------
# インストール結果の確認
# ------------------------------------------------------------

echo
echo "Installed packages:"

dpkg -l \
  | grep '^ii' \
  | grep 'linux-edge-inspection' \
  || true

# ------------------------------------------------------------
# インストール後の案内
# ------------------------------------------------------------

echo
echo "Installation completed."

echo
echo "Services are not automatically started."

echo
echo "Example:"
echo "  sudo systemctl start linux-edge-inspection-capture-request-listener.service"
echo "  sudo systemctl start linux-edge-inspection-inspection-worker.service"
echo "  sudo systemctl start linux-edge-inspection-management.service"

echo
echo "Management UI:"
echo "  http://<server-ip>:8080"