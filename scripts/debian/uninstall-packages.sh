#!/usr/bin/env bash

# Linux Edge InspectionのDebian Packageをまとめてアンインストールします。
#
# 対象パッケージ:
# - linux-edge-inspection-inspection-worker
# - linux-edge-inspection-capture-request-listener
# - linux-edge-inspection-runtime
#
# アンインストール順は依存関係の上位から、
# Inspection Worker → Capture Request Listener → Runtime
# とします。

set -euo pipefail

# ------------------------------------------------------------
# パッケージ名
# ------------------------------------------------------------

WORKER_PACKAGE="linux-edge-inspection-inspection-worker"
LISTENER_PACKAGE="linux-edge-inspection-capture-request-listener"
RUNTIME_PACKAGE="linux-edge-inspection-runtime"

# ------------------------------------------------------------
# サービス停止
# ------------------------------------------------------------

echo "Stopping Linux Edge Inspection services..."

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
# Debian Packageのアンインストール
# ------------------------------------------------------------

echo
echo "Removing Linux Edge Inspection packages..."

sudo apt-get remove -y \
  "${WORKER_PACKAGE}" \
  "${LISTENER_PACKAGE}" \
  "${RUNTIME_PACKAGE}"

# ------------------------------------------------------------
# systemd定義の再読み込み
# ------------------------------------------------------------

sudo systemctl daemon-reload

# ------------------------------------------------------------
# アンインストール結果の確認
# ------------------------------------------------------------

echo
echo "Remaining Linux Edge Inspection packages:"

dpkg -l | grep 'linux-edge-inspection' || true

echo
echo "Uninstallation completed."
echo
echo "Note:"
echo "  /var/lib/linux-edge-inspection-runtime may remain because"
echo "  captured images and application state are not removed automatically."
