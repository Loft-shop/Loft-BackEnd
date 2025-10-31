#!/bin/bash

# Скрипт быстрой настройки сервера для деплоя Loft-BackEnd
# Запустите этот скрипт на вашем сервере

set -e

echo "=== Настройка сервера для Loft-BackEnd ==="

# Проверка запуска от root
if [ "$EUID" -ne 0 ]; then 
    echo "Пожалуйста, запустите скрипт с sudo"
    exit 1
fi

# Обновление системы
echo "Обновление системы..."
apt-get update
apt-get upgrade -y

# Установка Docker
if ! command -v docker &> /dev/null; then
    echo "Установка Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    rm get-docker.sh
else
    echo "Docker уже установлен"
fi

# Установка Docker Compose
if ! docker compose version &> /dev/null; then
    echo "Установка Docker Compose..."
    apt-get install -y docker-compose-plugin
else
    echo "Docker Compose уже установлен"
fi

# Добавление текущего пользователя в группу docker
if [ -n "$SUDO_USER" ]; then
    usermod -aG docker $SUDO_USER
    echo "Пользователь $SUDO_USER добавлен в группу docker"
fi

# Создание директории для проекта
DEPLOY_PATH="/opt/loft-backend"
echo "Создание директории $DEPLOY_PATH..."
mkdir -p $DEPLOY_PATH
chown -R $SUDO_USER:$SUDO_USER $DEPLOY_PATH

# Настройка файрвола
echo "Настройка файрвола..."
if command -v ufw &> /dev/null; then
    ufw allow 22/tcp    # SSH
    ufw allow 5000/tcp  # API Gateway
    ufw --force enable
    echo "Файрвол настроен"
fi

# Создание swap файла (если нужно)
if [ $(free -m | awk '/^Swap:/ {print $2}') -eq 0 ]; then
    echo "Создание swap файла (2GB)..."
    fallocate -l 2G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
    echo "Swap файл создан"
fi

# Вывод информации
echo ""
echo "=== Настройка завершена! ==="
echo ""
echo "Следующие шаги:"
echo "1. Разлогиньтесь и залогиньтесь снова (для применения группы docker)"
echo "2. Создайте файл $DEPLOY_PATH/.env с настройками"
echo "3. Настройте GitHub Actions secrets в вашем репозитории"
echo ""
echo "Директория проекта: $DEPLOY_PATH"
echo "Версия Docker: $(docker --version)"
echo "Версия Docker Compose: $(docker compose version)"
echo ""
echo "Откройте порты в облачном провайдере (если используете):"
echo "  - 22 (SSH)"
echo "  - 5000 (API Gateway)"

