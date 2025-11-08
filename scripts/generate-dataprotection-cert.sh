#!/bin/bash

# Скрипт для генерации Data Protection сертификата
# Использование: ./scripts/generate-dataprotection-cert.sh [password]

set -e

CERT_DIR="./certs"
CERT_NAME="dataprotection"
CERT_FILE="${CERT_DIR}/${CERT_NAME}.pfx"
CERT_PASSWORD="${1:-}"

echo "🔐 Генерация Data Protection сертификата..."
echo ""

# Создаём директорию для сертификатов
mkdir -p "$CERT_DIR"

# Проверяем, существует ли уже сертификат
if [ -f "$CERT_FILE" ]; then
    echo "⚠️  ВНИМАНИЕ: Сертификат уже существует: $CERT_FILE"
    read -p "Перезаписать? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Отменено."
        exit 0
    fi
    rm -f "$CERT_FILE"
fi

# Генерируем пароль, если не указан
if [ -z "$CERT_PASSWORD" ]; then
    CERT_PASSWORD=$(openssl rand -base64 32)
    echo "🔑 Сгенерирован пароль для сертификата"
    echo "   Сохраните его в .env как DATA_PROTECTION_CERT_PASSWORD"
    echo ""
fi

# Генерируем самоподписанный сертификат
echo "📜 Создание сертификата..."

openssl req -x509 -newkey rsa:2048 -keyout "${CERT_DIR}/${CERT_NAME}.key" \
    -out "${CERT_DIR}/${CERT_NAME}.crt" -days 3650 -nodes \
    -subj "/CN=TFinanceDataProtection/O=TFinance/C=RU" \
    -addext "keyUsage=keyEncipherment,dataEncipherment" \
    -addext "extendedKeyUsage=serverAuth"

# Конвертируем в PFX формат
openssl pkcs12 -export -out "$CERT_FILE" \
    -inkey "${CERT_DIR}/${CERT_NAME}.key" \
    -in "${CERT_DIR}/${CERT_NAME}.crt" \
    -passout "pass:${CERT_PASSWORD}" \
    -name "TFinanceDataProtection"

# Удаляем временные файлы
rm -f "${CERT_DIR}/${CERT_NAME}.key" "${CERT_DIR}/${CERT_NAME}.crt"

# Устанавливаем правильные права доступа
chmod 600 "$CERT_FILE"

echo ""
echo "✅ Сертификат успешно создан: $CERT_FILE"
echo ""
echo "📋 Информация:"
echo "   Файл: $CERT_FILE"
echo "   Пароль: ${CERT_PASSWORD:0:10}... (полный пароль сохранён выше)"
echo "   Срок действия: 10 лет"
echo ""
echo "⚠️  ВАЖНО:"
echo "   1. Добавьте пароль в .env: DATA_PROTECTION_CERT_PASSWORD=${CERT_PASSWORD}"
echo "   2. Убедитесь, что $CERT_DIR/ добавлен в .gitignore"
echo "   3. Сохраните пароль в безопасном месте!"
echo ""

