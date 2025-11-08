#!/bin/bash

# Скрипт для загрузки SSL сертификатов из Selectel S3 или секретов
# Использование: ./scripts/download-ssl.sh

set -e

SSL_DIR="./ssl"
mkdir -p "$SSL_DIR"

echo "Загрузка SSL сертификатов для t-finance-web.ru..."

# Вариант 1: Загрузка из Selectel S3
# Раскомментируйте и настройте под вашу конфигурацию:
#
# export AWS_ACCESS_KEY_ID="your-access-key"
# export AWS_SECRET_ACCESS_KEY="your-secret-key"
# export AWS_ENDPOINT_URL="https://s3.selcdn.ru"
#
# aws s3 cp s3://your-bucket/ssl/t-finance-web.ru/fullchain.pem "$SSL_DIR/fullchain.pem" \
#     --endpoint-url "$AWS_ENDPOINT_URL"
# aws s3 cp s3://your-bucket/ssl/t-finance-web.ru/privkey.pem "$SSL_DIR/privkey.pem" \
#     --endpoint-url "$AWS_ENDPOINT_URL"

# Вариант 2: Загрузка через Selectel API (секреты)
# Раскомментируйте и настройте:
#
# SELECTEL_TOKEN="your-token"
# PROJECT_ID="your-project-id"
# SECRET_NAME="ssl-t-finance-web-ru"
#
# # Получение секрета через API
# curl -X GET \
#   "https://api.selectel.com/vpc/v2/secrets/$SECRET_NAME" \
#   -H "X-Token: $SELECTEL_TOKEN" \
#   -H "X-Project-Id: $PROJECT_ID" \
#   | jq -r '.value' | base64 -d > "$SSL_DIR/fullchain.pem"

# Вариант 3: Ручная загрузка
# Если сертификаты уже скачаны вручную, просто убедитесь, что они находятся в ./ssl/

if [ ! -f "$SSL_DIR/fullchain.pem" ] || [ ! -f "$SSL_DIR/privkey.pem" ]; then
    echo "ОШИБКА: Файлы сертификатов не найдены в $SSL_DIR/"
    echo "Пожалуйста, скачайте сертификаты из Selectel и поместите их в директорию ssl/"
    echo "Требуемые файлы: fullchain.pem и privkey.pem"
    exit 1
fi

# Установка правильных прав доступа
chmod 644 "$SSL_DIR/fullchain.pem"
chmod 600 "$SSL_DIR/privkey.pem"

# Проверка сертификатов
echo "Проверка сертификатов..."

if ! openssl x509 -in "$SSL_DIR/fullchain.pem" -text -noout > /dev/null 2>&1; then
    echo "ОШИБКА: Неверный формат fullchain.pem"
    exit 1
fi

if ! openssl rsa -in "$SSL_DIR/privkey.pem" -check > /dev/null 2>&1; then
    echo "ОШИБКА: Неверный формат privkey.pem"
    exit 1
fi

echo "Сертификаты успешно загружены и проверены!"

