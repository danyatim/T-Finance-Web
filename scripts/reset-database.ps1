# PowerShell скрипт для удаления базы данных в продакшене
# Использование: .\scripts\reset-database.ps1

Write-Host "Остановка контейнера backend..." -ForegroundColor Yellow
docker-compose stop backend

Write-Host "Удаление базы данных из контейнера..." -ForegroundColor Yellow
docker-compose run --rm backend sh -c "rm -f /app/Data/users.db /app/Data/users.dev.db 2>/dev/null; echo 'База данных удалена'"

Write-Host "Запуск контейнера backend..." -ForegroundColor Yellow
docker-compose up -d backend

Write-Host "Готово! База данных будет создана заново при следующем запуске." -ForegroundColor Green

