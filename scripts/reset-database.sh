#!/bin/bash

# Скрипт для удаления базы данных в продакшене
# Использование: ./scripts/reset-database.sh

echo "Остановка контейнера backend..."
docker-compose stop backend

echo "Удаление базы данных из контейнера..."
docker-compose run --rm backend sh -c "rm -f /app/Data/users.db /app/Data/users.dev.db 2>/dev/null; echo 'База данных удалена'"

echo "Запуск контейнера backend..."
docker-compose up -d backend

echo "Готово! База данных будет создана заново при следующем запуске."

