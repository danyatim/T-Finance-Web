# SSL Сертификаты

Эта директория должна содержать SSL сертификаты для домена `t-finance-web.ru`.

**Важно:** Директория `ssl/` должна находиться рядом с директорией проекта `T-Finance-Web/` (на один уровень выше).

Структура должна быть такой:
```
/
├── T-Finance-Web/          (проект)
│   ├── docker-compose.yml
│   ├── T-FinanceBackend/
│   └── T-FinanceFrontend/
└── ssl/                    (сертификаты - здесь!)
    ├── fullchain.pem
    └── privkey.pem
```

## Требуемые файлы:

- `fullchain.pem` - полная цепочка сертификатов (certificate + chain)
- `privkey.pem` - приватный ключ

## Как получить сертификаты из Selectel:

### Вариант 1: Из S3

```bash
# Установите AWS CLI (если еще не установлен)
# Настройте credentials для Selectel S3
aws configure --profile selectel

# Скачайте сертификаты
aws s3 cp s3://your-bucket/ssl/t-finance-web.ru/fullchain.pem ./ssl/fullchain.pem --profile selectel
aws s3 cp s3://your-bucket/ssl/t-finance-web.ru/privkey.pem ./ssl/privkey.pem --profile selectel

# Установите правильные права доступа
chmod 644 ./ssl/fullchain.pem
chmod 600 ./ssl/privkey.pem
```

### Вариант 2: Из менеджера секретов Selectel

```bash
# Используйте Selectel CLI или API для получения секретов
# Сохраните сертификаты в эту директорию как fullchain.pem и privkey.pem
```

### Вариант 3: Вручную

1. Скачайте сертификаты из панели Selectel
2. Сохраните их в эту директорию:
   - `fullchain.pem` - полный сертификат с цепочкой
   - `privkey.pem` - приватный ключ

## Проверка сертификатов:

```bash
# Проверьте формат сертификата
openssl x509 -in ./ssl/fullchain.pem -text -noout

# Проверьте формат ключа
openssl rsa -in ./ssl/privkey.pem -check

# Проверьте соответствие ключа и сертификата
openssl x509 -noout -modulus -in ./ssl/fullchain.pem | openssl md5
openssl rsa -noout -modulus -in ./ssl/privkey.pem | openssl md5
# MD5 хеши должны совпадать
```

## Важно:

- Файлы должны иметь правильные права доступа (644 для fullchain.pem, 600 для privkey.pem)
- Директория `ssl/` должна существовать перед запуском docker-compose
- Сертификаты монтируются в контейнер только для чтения (`:ro`)

