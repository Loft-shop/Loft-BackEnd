# Инструкция по настройке CI/CD пайплайна

## Обзор

Этот пайплайн автоматически деплоит ваш проект на сервер при каждом пуше в ветку `main` или `master`.

## Что делает пайплайн:

1. **Build and Test** - Проверяет код, собирает проект и запускает тесты
2. **Deploy** - Деплоит приложение на ваш сервер через SSH

## Настройка

### 1. Подготовка сервера

На вашем сервере должно быть установлено:
- Docker
- Docker Compose

Установите их, если еще не установлены:

```bash
# Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Docker Compose (обычно идет с Docker)
sudo apt-get update
sudo apt-get install docker-compose-plugin
```

### 2. Создание SSH ключа для деплоя

На вашем локальном компьютере создайте SSH ключ:

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_deploy
```

Скопируйте публичный ключ на сервер:

```bash
ssh-copy-id -i ~/.ssh/github_deploy.pub your_user@your_server_ip
```

Или вручную добавьте содержимое `~/.ssh/github_deploy.pub` в файл `~/.ssh/authorized_keys` на сервере.

### 3. Настройка GitHub Secrets

Перейдите в настройки вашего репозитория на GitHub:
`Settings → Secrets and variables → Actions → New repository secret`

Добавьте следующие секреты:

#### SSH_PRIVATE_KEY
Содержимое приватного ключа:
```bash
cat ~/.ssh/github_deploy
```
Скопируйте весь вывод (включая `-----BEGIN OPENSSH PRIVATE KEY-----` и `-----END OPENSSH PRIVATE KEY-----`)

#### SERVER_HOST
IP адрес или домен вашего сервера, например:
```
123.45.67.89
```
или
```
myserver.example.com
```

#### SERVER_USER
Имя пользователя для подключения к серверу, например:
```
ubuntu
```
или
```
root
```

#### DEPLOY_PATH
Путь на сервере, куда будет деплоиться проект, например:
```
/home/ubuntu/loft-backend
```
или
```
/opt/loft-backend
```

### 4. Создание директории на сервере

Подключитесь к серверу и создайте директорию для проекта:

```bash
ssh your_user@your_server_ip
mkdir -p /home/ubuntu/loft-backend  # или другой путь из DEPLOY_PATH
exit
```

### 5. Настройка переменных окружения на сервере

Создайте файл `.env` на сервере в директории проекта:

```bash
ssh your_user@your_server_ip
cd /home/ubuntu/loft-backend
nano .env
```

Добавьте необходимые переменные окружения:

```env
POSTGRES_USER=developer
POSTGRES_PASSWORD=your_secure_password
POSTGRES_DB=test_loft_shop
ASPNETCORE_ENVIRONMENT=Production
```

### 6. Настройка файрвола (опционально)

Откройте необходимые порты на сервере:

```bash
sudo ufw allow 5000/tcp  # API Gateway
sudo ufw allow 22/tcp    # SSH
sudo ufw enable
```

## Использование

После настройки просто пушьте код в репозиторий:

```bash
git add .
git commit -m "Your changes"
git push origin main
```

Пайплайн автоматически:
1. Соберет проект
2. Запустит тесты
3. Задеплоит на сервер
4. Перезапустит Docker контейнеры

## Проверка статуса деплоя

1. На GitHub перейдите в раздел **Actions**
2. Выберите последний запуск пайплайна
3. Посмотрите логи выполнения

## Проверка на сервере

Подключитесь к серверу и проверьте статус:

```bash
ssh your_user@your_server_ip
cd /home/ubuntu/loft-backend
docker compose ps
docker compose logs -f apigateway
```

## Откат к предыдущей версии

Если что-то пошло не так:

```bash
ssh your_user@your_server_ip
cd /home/ubuntu/loft-backend

# Откатитесь к предыдущему коммиту
git log --oneline  # найдите нужный коммит
git checkout <commit-hash>

# Перезапустите
./scripts/deploy.sh
```

## Ручной деплой

Если нужно задеплоить вручную:

```bash
ssh your_user@your_server_ip
cd /home/ubuntu/loft-backend
git pull
./scripts/deploy.sh
```

## Мониторинг логов

```bash
# Все сервисы
docker compose logs -f

# Конкретный сервис
docker compose logs -f apigateway
docker compose logs -f productservice

# Последние 100 строк
docker compose logs --tail=100
```

## Устранение проблем

### Проблема: Контейнеры не запускаются

```bash
docker compose ps
docker compose logs
```

### Проблема: Порты заняты

```bash
sudo netstat -tulpn | grep LISTEN
# Остановите процессы, занимающие нужные порты
```

### Проблема: Недостаточно места на диске

```bash
docker system prune -a --volumes
```

### Проблема: Ошибки SSH

Проверьте:
- Правильность SSH ключа в GitHub Secrets
- Доступность сервера по SSH
- Права на директорию деплоя

```bash
# Проверка SSH подключения
ssh -i ~/.ssh/github_deploy your_user@your_server_ip
```

## Дополнительные настройки

### Уведомления в Telegram/Slack

Добавьте в конец пайплайна шаг для отправки уведомлений о результате деплоя.

### Бэкап перед деплоем

Добавьте в `scripts/deploy.sh` создание бэкапа базы данных перед обновлением.

### Деплой в разные окружения

Создайте отдельные пайплайны для staging и production окружений.

## Безопасность

- ✅ Используйте отдельный SSH ключ только для деплоя
- ✅ Ограничьте права пользователя на сервере
- ✅ Используйте сильные пароли для баз данных
- ✅ Регулярно обновляйте зависимости
- ✅ Не коммитьте секреты в репозиторий

