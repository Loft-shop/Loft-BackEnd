# 🚀 CI/CD Pipeline - Быстрый старт

Автоматический деплой вашего проекта на сервер готов!

## 📁 Что было создано:

### Пайплайны (выберите один):
- `.github/workflows/deploy.yml` - **Простой вариант** (рекомендуется для начала)
- `.github/workflows/deploy-with-registry.yml` - С Docker Hub (для production)

### Документация:
- `.github/README_CICD.md` - 📖 **НАЧНИТЕ ОТСЮДА** - Быстрый старт
- `.github/DEPLOYMENT.md` - Полная инструкция
- `.github/CHECKLIST.md` - Чеклист настройки

### Скрипты:
- `scripts/deploy.sh` - Скрипт деплоя на сервере
- `scripts/setup-server.sh` - Автоматическая настройка сервера

### Конфигурация:
- `compose.production.yaml` - Docker Compose для продакшна
- `.env.production.example` - Пример переменных окружения

---

## ⚡ Быстрая настройка (5 минут):

### 1️⃣ На локальном компьютере:

```bash
# Создайте SSH ключ
ssh-keygen -t ed25519 -C "github-deploy" -f ~/.ssh/github_deploy

# Скопируйте на сервер (замените username и server-ip)
ssh-copy-id -i ~/.ssh/github_deploy.pub username@server-ip
```

### 2️⃣ На сервере:

```bash
# Скопируйте и запустите скрипт настройки
scp scripts/setup-server.sh username@server-ip:~
ssh username@server-ip
sudo bash setup-server.sh
```

### 3️⃣ В GitHub:

Откройте: **Settings → Secrets and variables → Actions → New repository secret**

Добавьте секреты:
- `SSH_PRIVATE_KEY` → содержимое `~/.ssh/github_deploy`
- `SERVER_HOST` → `123.45.67.89` (ваш IP)
- `SERVER_USER` → `ubuntu` (ваш пользователь)
- `DEPLOY_PATH` → `/opt/loft-backend`

### 4️⃣ Пушьте код:

```bash
git add .
git commit -m "Setup CI/CD"
git push origin main
```

**Готово!** 🎉 Теперь при каждом пуше код автоматически деплоится на сервер.

---

## 📚 Что дальше?

1. Откройте `.github/README_CICD.md` - там подробная инструкция
2. Следуйте чеклисту из `.github/CHECKLIST.md`
3. Проверьте деплой в разделе **Actions** на GitHub

## 🆘 Нужна помощь?

- Все работает автоматически после настройки
- Логи деплоя смотрите в GitHub Actions
- Логи на сервере: `ssh user@server && docker compose logs`

**Успешного деплоя! 🚀**

