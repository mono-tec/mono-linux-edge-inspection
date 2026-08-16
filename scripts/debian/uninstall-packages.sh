#!/usr/bin/env bash

# Linux Edge InspectionのDebian Packageをまとめてアンインストールします。
#
# 対象パッケージ:
# - linux-edge-inspection-management
# - linux-edge-inspection-inspection-worker
# - linux-edge-inspection-capture-request-listener
# - linux-edge-inspection-runtime
#
# アンインストール順は依存関係や利用関係を考慮し、
# Management → Inspection Worker → Capture Request Listener → Runtime
# とします。

set -euo pipefail

# ------------------------------------------------------------
# パッケージ名
# ------------------------------------------------------------

MANAGEMENT_PACKAGE="linux-edge-inspection-management"
WORKER_PACKAGE="linux-edge-inspection-inspection-worker"
LISTENER_PACKAGE="linux-edge-inspection-capture-request-listener"
RUNTIME_PACKAGE="linux-edge-inspection-runtime"

# ------------------------------------------------------------
# サービス停止
# ------------------------------------------------------------

echo "Stopping Linux Edge Inspection services..."

# Management UIを停止します。
sudo systemctl stop \
  linux-edge-inspection-management.service \
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

# 上位側のManagement / Workerから順に指定します。
sudo apt-get remove -y \
  "${MANAGEMENT_PACKAGE}" \
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

dpkg -l \
  | grep 'linux-edge-inspection' \
  || true

# ------------------------------------------------------------
# 完了メッセージ
# ------------------------------------------------------------

echo
echo "Uninstallation completed."

echo
echo "Note:"
echo "  /var/lib/linux-edge-inspection-runtime may remain because"
echo "  captured images and application state are not removed automatically."