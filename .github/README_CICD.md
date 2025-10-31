# CI/CD Setup - Краткое руководство

## Два варианта деплоя

Я создал для вас **два варианта** пайплайна. Выберите тот, который вам больше подходит:

### 🚀 Вариант 1: Простой деплой (deploy.yml)
**Рекомендуется для начала**

- Код копируется на сервер через SSH
- Docker образы собираются на самом сервере
- Проще в настройке

**Минусы:** Требует больше ресурсов на сервере для сборки

### 🐳 Вариант 2: С Docker Registry (deploy-with-registry.yml)
**Для production и масштабирования**

- Docker образы собираются в GitHub Actions
- Образы загружаются в Docker Hub
- Сервер только скачивает готовые образы

**Плюсы:** Быстрее деплой, меньше нагрузка на сервер, можно использовать несколько серверов

---

## Быстрый старт

### Для варианта 1 (Простой):

1. **Создайте SSH ключ:**
```bash
ssh-keygen -t ed25519 -C "github-deploy" -f ~/.ssh/github_deploy
```

2. **Скопируйте ключ на сервер:**
```bash
ssh-copy-id -i ~/.ssh/github_deploy.pub username@your-server-ip
```

3. **Добавьте секреты в GitHub:**
   - `Settings → Secrets and variables → Actions → New repository secret`
   
   Добавьте:
   - `SSH_PRIVATE_KEY` - содержимое файла `~/.ssh/github_deploy`
   - `SERVER_HOST` - IP или домен сервера (например: `123.45.67.89`)
   - `SERVER_USER` - пользователь SSH (например: `ubuntu`)
   - `DEPLOY_PATH` - путь на сервере (например: `/home/ubuntu/loft-backend`)

4. **На сервере создайте директорию:**
```bash
mkdir -p /home/ubuntu/loft-backend
```

5. **Отключите второй пайплайн (временно):**
   - Переименуйте `deploy-with-registry.yml` в `deploy-with-registry.yml.disabled`

6. **Пушьте код:**
```bash
git add .
git commit -m "Setup CI/CD"
git push origin main
```

### Для варианта 2 (С Docker Hub):

1. **Зарегистрируйтесь на Docker Hub:** https://hub.docker.com

2. **Создайте SSH ключ** (как в варианте 1)

3. **Добавьте секреты в GitHub:**
   Все из варианта 1, плюс:
   - `DOCKER_USERNAME` - ваш username на Docker Hub
   - `DOCKER_PASSWORD` - пароль или токен от Docker Hub

4. **На сервере:**
```bash
mkdir -p /home/ubuntu/loft-backend
cd /home/ubuntu/loft-backend

# Скопируйте файлы
scp compose.production.yaml your-server:/home/ubuntu/loft-backend/compose.yaml
scp .env.production.example your-server:/home/ubuntu/loft-backend/.env

# На сервере отредактируйте .env
nano .env
```

5. **Отключите первый пайплайн:**
   - Переименуйте `deploy.yml` в `deploy.yml.disabled`

6. **Пушьте код**

---

## Проверка работы

После пуша:
1. Откройте вкладку **Actions** в GitHub
2. Следите за процессом выполнения
3. После завершения проверьте сервер:

```bash
ssh username@server-ip
docker ps
curl http://localhost:5000/health  # если есть health endpoint
```

---

## Что дальше?

- 📖 Полная инструкция: `.github/DEPLOYMENT.md`
- 🔧 Настройка переменных окружения на сервере
- 🔒 Настройка HTTPS (рекомендуется nginx + Let's Encrypt)
- 📊 Мониторинг и логирование

## Помощь

Если что-то не работает:
1. Проверьте логи в GitHub Actions
2. Подключитесь к серверу и проверьте `docker compose logs`
3. Убедитесь, что все секреты правильно настроены

