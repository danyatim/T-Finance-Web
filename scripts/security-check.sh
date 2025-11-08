#!/bin/bash

# Скрипт для автоматической проверки безопасности приложения
# Использование: ./scripts/security-check.sh

set -e

echo "🔒 Начинаем проверку безопасности..."
echo ""

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Функция для вывода ошибок
error() {
    echo -e "${RED}❌ $1${NC}"
}

# Функция для вывода успеха
success() {
    echo -e "${GREEN}✅ $1${NC}"
}

# Функция для вывода предупреждений
warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

echo "=== 1. Проверка зависимостей ==="

# Проверка .NET зависимостей
if command -v dotnet &> /dev/null; then
    echo "Проверка уязвимостей в .NET пакетах..."
    cd T-FinanceBackend
    if dotnet list package --vulnerable 2>/dev/null | grep -q "vulnerable"; then
        error "Найдены уязвимые пакеты в .NET проекте"
        dotnet list package --vulnerable
    else
        success "Уязвимых пакетов в .NET проекте не найдено"
    fi
    cd ..
else
    warning "dotnet не установлен, пропускаем проверку .NET зависимостей"
fi

# Проверка Node.js зависимостей
if command -v npm &> /dev/null; then
    echo "Проверка уязвимостей в Node.js пакетах..."
    cd T-FinanceFrontend
    if npm audit --audit-level=moderate 2>/dev/null | grep -q "found"; then
        error "Найдены уязвимости в Node.js пакетах"
        npm audit
    else
        success "Критических уязвимостей в Node.js пакетах не найдено"
    fi
    cd ..
else
    warning "npm не установлен, пропускаем проверку Node.js зависимостей"
fi

echo ""
echo "=== 2. Проверка конфигурации ==="

# Проверка наличия секретов в коде
if grep -r "WB5UwGbNhrBDKgV210mAG04AGbdGUL1rWTldXBg3" T-FinanceBackend/appsettings.json 2>/dev/null; then
    error "JWT секретный ключ найден в appsettings.json - должен быть в переменных окружения!"
else
    success "JWT ключ не найден в appsettings.json"
fi

# Проверка наличия паролей в коде
if grep -ri "password.*=" T-FinanceBackend/ --include="*.cs" 2>/dev/null | grep -v "PasswordHash" | grep -v "PasswordHasher"; then
    warning "Найдены возможные хардкод пароли в коде"
else
    success "Хардкод паролей не найден"
fi

echo ""
echo "=== 3. Проверка Docker конфигурации ==="

# Проверка, что контейнеры не запускаются от root
if grep -q "USER root" T-FinanceBackend/Dockerfile 2>/dev/null; then
    warning "Backend контейнер запускается от root"
else
    success "Backend контейнер не запускается от root"
fi

if grep -q "USER root" T-FinanceFrontend/Dockerfile 2>/dev/null; then
    warning "Frontend контейнер запускается от root"
else
    success "Frontend контейнер не запускается от root (nginx по умолчанию)"
fi

echo ""
echo "=== 4. Проверка SSL/TLS ==="

DOMAIN="t-finance-web.ru"

if command -v openssl &> /dev/null; then
    echo "Проверка SSL сертификата для $DOMAIN..."
    if echo | openssl s_client -connect $DOMAIN:443 -servername $DOMAIN 2>/dev/null | grep -q "Verify return code: 0"; then
        success "SSL сертификат валиден"
    else
        error "Проблемы с SSL сертификатом"
    fi
else
    warning "openssl не установлен, пропускаем проверку SSL"
fi

echo ""
echo "=== 5. Проверка Security Headers ==="

if command -v curl &> /dev/null; then
    echo "Проверка security headers..."
    HEADERS=$(curl -sI "https://$DOMAIN" 2>/dev/null || echo "")
    
    if echo "$HEADERS" | grep -qi "Strict-Transport-Security"; then
        success "HSTS заголовок присутствует"
    else
        error "HSTS заголовок отсутствует"
    fi
    
    if echo "$HEADERS" | grep -qi "X-Frame-Options"; then
        success "X-Frame-Options заголовок присутствует"
    else
        error "X-Frame-Options заголовок отсутствует"
    fi
    
    if echo "$HEADERS" | grep -qi "X-Content-Type-Options"; then
        success "X-Content-Type-Options заголовок присутствует"
    else
        error "X-Content-Type-Options заголовок отсутствует"
    fi
else
    warning "curl не установлен, пропускаем проверку headers"
fi

echo ""
echo "=== 6. Рекомендации ==="
echo ""
warning "Рекомендуется:"
echo "  1. Перенести JWT секреты в переменные окружения"
echo "  2. Добавить Rate Limiting для защиты от брутфорса"
echo "  3. Настроить валидацию паролей (минимум 8 символов, сложность)"
echo "  4. Использовать SameSite=Strict для cookies в production"
echo "  5. Настроить регулярное обновление зависимостей"
echo ""
echo "Подробнее см. SECURITY.md"

