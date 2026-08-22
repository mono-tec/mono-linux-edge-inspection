#!/usr/bin/env bash

# Linux Edge Inspection の最新 GitHub Release を取得し、
# 実行環境の CPU Architecture に対応した 6 つの Debian Package を
# まとめてダウンロード・インストールします。
#
# 対応 Architecture:
# - amd64
# - arm64
#
# 対象パッケージ:
# - linux-edge-inspection-runtime
# - linux-edge-inspection-capture-request-listener
# - linux-edge-inspection-inspection-worker
# - linux-edge-inspection-management
# - linux-edge-inspection-management-api
# - linux-edge-inspection-image-cleanup

set -euo pipefail

# ------------------------------------------------------------
# 基本設定
# ------------------------------------------------------------

REPOSITORY_OWNER="mono-tec"
REPOSITORY_NAME="mono-linux-edge-inspection"

GITHUB_RELEASE_BASE_URL="https://github.com/${REPOSITORY_OWNER}/${REPOSITORY_NAME}/releases"

# ------------------------------------------------------------
# 必要コマンドの確認
# ------------------------------------------------------------

# Architecture 判定に最初に使用するため、dpkg を先に確認します。
if ! command -v dpkg >/dev/null 2>&1; then
  echo "ERROR: Required command not found: dpkg"
  exit 1
fi

for command_name in curl wget apt-get; do
  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "ERROR: Required command not found: ${command_name}"

    if [[ "${command_name}" == "curl" ]]; then
      echo
      echo "Install curl first:"
      echo "  sudo apt-get update"
      echo "  sudo apt-get install -y curl"
    fi

    exit 1
  fi
done

# ------------------------------------------------------------
# CPU Architecture の判定
# ------------------------------------------------------------

DETECTED_ARCHITECTURE="$(dpkg --print-architecture)"

case "${DETECTED_ARCHITECTURE}" in
  amd64)
    ARCHITECTURE="amd64"
    ;;

  arm64)
    ARCHITECTURE="arm64"
    ;;

  *)
    echo "ERROR: Unsupported architecture: ${DETECTED_ARCHITECTURE}"
    echo "Supported architectures: amd64, arm64"
    exit 1
    ;;
esac

echo "Detected architecture: ${ARCHITECTURE}"

# ------------------------------------------------------------
# 最新 Release タグの取得
# ------------------------------------------------------------

echo
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
  echo "ERROR: Failed to determine latest release tag."
  exit 1
fi

# Debian Package の Version では先頭の v を除去します。
#
# 例:
#   v0.4.4
#     ↓
#   0.4.4
PACKAGE_VERSION="${LATEST_TAG#v}"

echo "Latest release: ${LATEST_TAG}"
echo "Package version: ${PACKAGE_VERSION}"
echo "Package architecture: ${ARCHITECTURE}"

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

MANAGEMENT_API_PACKAGE="linux-edge-inspection-management-api_${PACKAGE_VERSION}_${ARCHITECTURE}.deb"

IMAGE_CLEANUP_PACKAGE="linux-edge-inspection-image-cleanup_${PACKAGE_VERSION}_${ARCHITECTURE}.deb"

DOWNLOAD_BASE_URL="${GITHUB_RELEASE_BASE_URL}/download/${LATEST_TAG}"

# ------------------------------------------------------------
# 最新 Release から Debian Package をダウンロード
# ------------------------------------------------------------

download_package() {
  local package_name="$1"
  local package_url="${DOWNLOAD_BASE_URL}/${package_name}"

  echo
  echo "Downloading: ${package_name}"

  if ! wget \
    --quiet \
    --show-progress \
    "${package_url}"; then
    echo
    echo "ERROR: Failed to download package:"
    echo "  ${package_name}"
    echo
    echo "Check whether the GitHub Release contains the"
    echo "${ARCHITECTURE} package for version ${PACKAGE_VERSION}."
    exit 1
  fi
}

download_package "${RUNTIME_PACKAGE}"
download_package "${LISTENER_PACKAGE}"
download_package "${WORKER_PACKAGE}"
download_package "${MANAGEMENT_PACKAGE}"
download_package "${MANAGEMENT_API_PACKAGE}"
download_package "${IMAGE_CLEANUP_PACKAGE}"

echo
echo "Downloaded packages:"

ls -lh \
  "${RUNTIME_PACKAGE}" \
  "${LISTENER_PACKAGE}" \
  "${WORKER_PACKAGE}" \
  "${MANAGEMENT_PACKAGE}" \
  "${MANAGEMENT_API_PACKAGE}" \
  "${IMAGE_CLEANUP_PACKAGE}"

# ------------------------------------------------------------
# Debian Package の Architecture を確認
# ------------------------------------------------------------

echo
echo "Checking Debian package architectures..."

for package_name in \
  "${RUNTIME_PACKAGE}" \
  "${LISTENER_PACKAGE}" \
  "${WORKER_PACKAGE}" \
  "${MANAGEMENT_PACKAGE}" \
  "${MANAGEMENT_API_PACKAGE}" \
  "${IMAGE_CLEANUP_PACKAGE}"
do
  PACKAGE_ARCH="$(dpkg-deb -f "${package_name}" Architecture)"

  echo "  ${package_name}: ${PACKAGE_ARCH}"

  if [[ "${PACKAGE_ARCH}" != "${ARCHITECTURE}" ]]; then
    echo
    echo "ERROR: Package architecture mismatch."
    echo "Expected: ${ARCHITECTURE}"
    echo "Actual:   ${PACKAGE_ARCH}"
    exit 1
  fi
done

# ------------------------------------------------------------
# Debian Package をまとめてインストール
# ------------------------------------------------------------

echo
echo "Installing Linux Edge Inspection packages..."

# 6つを同時に apt-get へ渡すことで、
# ローカル deb 同士の依存関係と .NET Runtime などの依存関係を
# apt にまとめて解決させます。
sudo apt-get update

sudo apt-get install -y \
  "./${RUNTIME_PACKAGE}" \
  "./${LISTENER_PACKAGE}" \
  "./${WORKER_PACKAGE}" \
  "./${MANAGEMENT_PACKAGE}" \
  "./${MANAGEMENT_API_PACKAGE}" \
  "./${IMAGE_CLEANUP_PACKAGE}"

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
echo "Installed architecture: ${ARCHITECTURE}"
echo "Installed version: ${PACKAGE_VERSION}"

echo
echo "Services and timers are not automatically started."

echo
echo "Example:"
echo "  sudo systemctl start linux-edge-inspection-capture-request-listener.service"
echo "  sudo systemctl start linux-edge-inspection-inspection-worker.service"
echo "  sudo systemctl start linux-edge-inspection-management-api.service"
echo "  sudo systemctl start linux-edge-inspection-management.service"

echo
echo "Image Cleanup:"
echo "  Run once:"
echo "    sudo systemctl start linux-edge-inspection-image-cleanup.service"
echo
echo "  Enable scheduled cleanup:"
echo "    sudo systemctl enable --now linux-edge-inspection-image-cleanup.timer"
echo
echo "  Check timer:"
echo "    systemctl status linux-edge-inspection-image-cleanup.timer"
echo "    systemctl list-timers linux-edge-inspection-image-cleanup.timer"

echo
echo "Management UI:"
echo "  http://<server-ip>:8080"

echo
echo "Management API:"
echo "  http://<server-ip>:8081"
