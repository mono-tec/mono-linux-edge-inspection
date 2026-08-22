#!/usr/bin/env bash

set -euo pipefail

echo "========================================"
echo "Kakip Network Configuration"
echo "========================================"
echo

if [[ "${EUID}" -ne 0 ]]; then
    echo "ERROR: sudo で実行してください。"
    echo
    echo "例:"
    echo "  sudo ./scripts/configure-network.sh"
    exit 1
fi

echo "現在のネットワーク:"
echo
ip -br address
echo

echo "利用可能なインターフェース:"
echo

mapfile -t INTERFACES < <(
    ip -o link show |
    awk -F': ' '{print $2}' |
    sed 's/@.*//' |
    grep -Ev '^(lo|dummy[0-9]*|can[0-9]*|sit[0-9]*)$'
)

if [[ "${#INTERFACES[@]}" -eq 0 ]]; then
    echo "ERROR: 設定可能なネットワークインターフェースがありません。"
    exit 1
fi

for i in "${!INTERFACES[@]}"; do
    printf "%d. %s\n" "$((i + 1))" "${INTERFACES[$i]}"
done

echo
read -rp "設定するインターフェース番号を選択してください: " SELECTED

if ! [[ "${SELECTED}" =~ ^[0-9]+$ ]] ||
   (( SELECTED < 1 || SELECTED > ${#INTERFACES[@]} )); then
    echo "ERROR: 選択値が正しくありません。"
    exit 1
fi

IFACE="${INTERFACES[$((SELECTED - 1))]}"

echo
echo "選択:"
echo "  ${IFACE}"
echo

# SSH接続中の場合は、現在の接続経路を表示
if [[ -n "${SSH_CONNECTION:-}" ]]; then
    SSH_CLIENT_IP="$(awk '{print $1}' <<< "${SSH_CONNECTION}")"

    echo "現在SSH接続中です。"
    echo "SSH接続元:"
    echo "  ${SSH_CLIENT_IP}"
    echo
    echo "現在のSSH接続元へのルート:"
    ip route get "${SSH_CLIENT_IP}" 2>/dev/null || true
    echo

    read -rp "ネットワーク設定を変更しますか？ [y/N]: " CONFIRM

    if [[ ! "${CONFIRM}" =~ ^[Yy]$ ]]; then
        echo "キャンセルしました。"
        exit 0
    fi
fi

echo
echo "設定方式:"
echo
echo "1. DHCP"
echo "2. 固定IP"
echo
read -rp "選択してください: " MODE

case "${MODE}" in

    1)
        echo
        echo "DHCP設定を開始します。"

        # 既存IPv4アドレスを削除
        ip -4 addr flush dev "${IFACE}"

        ip link set "${IFACE}" up

        if command -v dhclient >/dev/null 2>&1; then

            echo "dhclient を使用します。"

            dhclient -r "${IFACE}" 2>/dev/null || true
            dhclient "${IFACE}"

        elif command -v networkctl >/dev/null 2>&1; then

            echo "networkctl でDHCP更新を試します。"

            networkctl renew "${IFACE}" || {
                echo
                echo "ERROR: DHCPアドレスを取得できませんでした。"
                echo "使用中のネットワーク管理方式を確認してください。"
                exit 1
            }

        else

            echo
            echo "ERROR: DHCPクライアントが見つかりません。"
            echo
            echo "確認候補:"
            echo "  dhclient"
            echo "  systemd-networkd"
            exit 1

        fi
        ;;

    2)
        echo
        read -rp "IPアドレス/CIDR (例: 192.168.X.10/24): " IP_CIDR
        read -rp "ネットワーク/CIDR (例: 192.168.X.0/24): " NETWORK_CIDR
        read -rp "Gateway（不要ならEnter）: " GATEWAY

        IP_ADDRESS="${IP_CIDR%/*}"

        echo
        echo "以下の内容で設定します。"
        echo
        echo "Interface : ${IFACE}"
        echo "Address   : ${IP_CIDR}"
        echo "Network   : ${NETWORK_CIDR}"

        if [[ -n "${GATEWAY}" ]]; then
            echo "Gateway   : ${GATEWAY}"
        else
            echo "Gateway   : なし"
        fi

        echo
        read -rp "実行しますか？ [y/N]: " CONFIRM

        if [[ ! "${CONFIRM}" =~ ^[Yy]$ ]]; then
            echo "キャンセルしました。"
            exit 0
        fi

        # 既存IPv4設定を削除
        ip -4 addr flush dev "${IFACE}"

        # InterfaceをUP
        ip link set "${IFACE}" up

        # IPv4アドレス設定
        ip addr add "${IP_CIDR}" dev "${IFACE}"

        # 接続ネットワークへのルートを明示
        ip route replace "${NETWORK_CIDR}" \
            dev "${IFACE}" \
            src "${IP_ADDRESS}"

        # Gateway指定時のみdefault routeを設定
        if [[ -n "${GATEWAY}" ]]; then
            ip route replace default \
                via "${GATEWAY}" \
                dev "${IFACE}"
        fi
        ;;

    *)
        echo "ERROR: 選択値が正しくありません。"
        exit 1
        ;;
esac

echo
echo "========================================"
echo "設定結果"
echo "========================================"
echo

ip -br address show dev "${IFACE}"

echo
echo "Route:"
ip route

echo
echo "設定完了しました。"