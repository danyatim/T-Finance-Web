# 🚀 Быстрый старт настройки на сервере

## Шаг 1: Создание .env файла

```bash
cd /path/to/T-Finance-Web

# Создайте .env файл
cat > .env << 'EOF'
# JWT настройки
JWT_KEY=ЗАМЕНИТЕ_НА_СГЕНЕРИРОВАННЫЙ_КЛЮЧ
JWT_ISSUER=TFinanceBackend
JWT_AUDIENCE=TFinanceFrontend
JWT_EXPIRES_IN_HOURS=1

# Data Protection (опционально)
DATA_PROTECTION_CERT_PATH=/app/certs/dataprotection.pfx
DATA_PROTECTION_CERT_PASSWORD=ЗАМЕНИТЕ_НА_ПАРОЛЬ
EOF

# Установите права доступа
chmod 600 .env
```

## Шаг 2: Генерация JWT ключа

```bash
# Генерируем ключ
JWT_KEY=$(openssl rand -base64 64)

# Добавляем в .env
sed -i "s|JWT_KEY=.*|JWT_KEY=$JWT_KEY|" .env

# Показываем ключ (сохраните его!)
echo "JWT_KEY=$JWT_KEY"
```

## Шаг 3: Создание Data Protection сертификата (опционально)

```bash
# Создаём директорию
mkdir -p certs

# Генерируем пароль
CERT_PASSWORD=$(openssl rand -base64 32)

# Генерируем сертификат
openssl req -x509 -newkey rsa:2048 \
    -keyout certs/temp.key -out certs/temp.crt \
    -days 3650 -nodes \
    -subj "/CN=TFinanceDataProtection/O=TFinance/C=RU"

# Конвертируем в PFX
openssl pkcs12 -export -out certs/dataprotection.pfx \
    -inkey certs/temp.key -in certs/temp.crt \
    -passout "pass:$CERT_PASSWORD" \
    -name "TFinanceDataProtection"

# Удаляем временные файлы
rm certs/temp.key certs/temp.crt

# Устанавливаем права
chmod 600 certs/dataprotection.pfx

# Добавляем пароль в .env
sed -i "s|DATA_PROTECTION_CERT_PASSWORD=.*|DATA_PROTECTION_CERT_PASSWORD=$CERT_PASSWORD|" .env

# Показываем пароль (сохраните его!)
echo "CERT_PASSWORD=$CERT_PASSWORD"
```

## Шаг 4: Запуск

```bash
docker compose up -d --build
```

## Шаг 5: Проверка

```bash
# Проверьте логи
docker compose logs backend | grep -E "(JWT|DataProtection|Certificate)"
```

Подробная инструкция: см. `SETUP_SERVER.md`

