# ✅ Чеклист настройки CI/CD

Используйте этот чеклист для проверки настройки деплоя.

## Подготовка сервера

- [ ] Сервер доступен по SSH
- [ ] Установлен Docker (`docker --version`)
- [ ] Установлен Docker Compose (`docker compose version`)
- [ ] Создана директория для проекта (например `/opt/loft-backend`)
- [ ] Открыты необходимые порты (22, 5000)

**Быстрая установка:** Скопируйте `scripts/setup-server.sh` на сервер и запустите с sudo

## SSH ключ

- [ ] Создан SSH ключ для деплоя
  ```bash
  ssh-keygen -t ed25519 -C "github-deploy" -f ~/.ssh/github_deploy
  ```
- [ ] Публичный ключ добавлен на сервер
  ```bash
  ssh-copy-id -i ~/.ssh/github_deploy.pub username@server-ip
  ```
- [ ] Проверено подключение
  ```bash
  ssh -i ~/.ssh/github_deploy username@server-ip
  ```

## GitHub Secrets

Откройте: `Settings → Secrets and variables → Actions`

### Для простого деплоя (deploy.yml):
- [ ] `SSH_PRIVATE_KEY` - содержимое `~/.ssh/github_deploy`
- [ ] `SERVER_HOST` - IP или домен (например: `123.45.67.89`)
- [ ] `SERVER_USER` - пользователь SSH (например: `ubuntu`)
- [ ] `DEPLOY_PATH` - путь на сервере (например: `/opt/loft-backend`)

### Дополнительно для Docker Hub (deploy-with-registry.yml):
- [ ] `DOCKER_USERNAME` - username на Docker Hub
- [ ] `DOCKER_PASSWORD` - пароль или токен

## Настройка на сервере

- [ ] Создан файл `.env` с настройками:
  ```bash
  cd /opt/loft-backend
  nano .env
  ```
  Минимум:
  - `POSTGRES_PASSWORD`
  - `DOCKER_USERNAME` (если используете Docker Hub)

## Выбор пайплайна

Выберите ОДИН из двух пайплайнов:

- [ ] **Вариант 1:** Оставить `deploy.yml` (простой)
- [ ] **Вариант 2:** Оставить `deploy-with-registry.yml` (с Docker Hub)
  
Второй пайплайн переименуйте добавив `.disabled` в конец файла

## Первый запуск

- [ ] Добавлены все изменения в git
  ```bash
  git add .
  git commit -m "Setup CI/CD pipeline"
  ```
- [ ] Код запушен в репозиторий
  ```bash
  git push origin main
  ```
- [ ] Открыта вкладка Actions на GitHub
- [ ] Пайплайн запущен и выполняется
- [ ] Проверены логи выполнения

## Проверка на сервере

После успешного деплоя:

- [ ] Подключиться к серверу
  ```bash
  ssh username@server-ip
  ```
- [ ] Проверить статус контейнеров
  ```bash
  cd /opt/loft-backend
  docker compose ps
  ```
- [ ] Все контейнеры в статусе "Up"
- [ ] Проверить логи
  ```bash
  docker compose logs --tail=50
  ```
- [ ] API Gateway отвечает
  ```bash
  curl http://localhost:5000
  ```

## Тестирование

- [ ] Сделать изменение в коде
- [ ] Запушить изменение
- [ ] Проверить автоматический деплой
- [ ] Изменения применились на сервере

## Дополнительно (рекомендуется)

- [ ] Настроить HTTPS (nginx + Let's Encrypt)
- [ ] Настроить резервное копирование БД
- [ ] Настроить мониторинг (Prometheus/Grafana)
- [ ] Настроить логирование (ELK/Loki)
- [ ] Настроить уведомления о деплое (Telegram/Slack)

## Полезные команды

### На сервере:
```bash
# Посмотреть статус
docker compose ps

# Логи в реальном времени
docker compose logs -f

# Перезапуск сервиса
docker compose restart apigateway

# Полная перезагрузка
docker compose down && docker compose up -d

# Очистка
docker system prune -a --volumes
```

### Локально:
```bash
# Посмотреть статус Actions
gh run list

# Посмотреть логи последнего запуска
gh run view --log
```

## Проблемы?

Если что-то не работает:
1. ✅ Проверьте логи в GitHub Actions
2. ✅ Проверьте логи на сервере: `docker compose logs`
3. ✅ Проверьте правильность всех секретов
4. ✅ Проверьте доступность сервера: `ping server-ip`
5. ✅ Проверьте SSH подключение
6. 📖 Читайте полную документацию: `.github/DEPLOYMENT.md`

