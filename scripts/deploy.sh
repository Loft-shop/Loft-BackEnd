#!/bin/bash

# Скрипт для деплоя на сервере
set -e

echo "=== Starting deployment ==="

# Переходим в директорию проекта
cd "$(dirname "$0")/.."

# Останавливаем текущие контейнеры
echo "Stopping containers..."
docker compose down

# Собираем новые образы
echo "Building new images..."
docker compose build --no-cache

# Запускаем контейнеры
echo "Starting containers..."
docker compose up -d

# Ждем запуска
echo "Waiting for services to start..."
sleep 10

# Проверяем статус
echo "Checking container status..."
docker compose ps

# Показываем логи
echo "Recent logs:"
docker compose logs --tail=50

# Очищаем старые образы
echo "Cleaning up old images..."
docker image prune -f

echo "=== Deployment completed successfully ==="

