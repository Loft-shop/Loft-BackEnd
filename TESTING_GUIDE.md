# 🧪 Руководство по тестированию Loft-BackEnd через Swagger

## 📋 Статус сервисов
Все сервисы успешно запущены и доступны для тестирования!

## 🌐 Swagger UI URLs

### API Gateway (главная точка входа) ⭐
- **URL**: http://localhost:5000/swagger
- **Описание**: Центральный шлюз для всех сервисов через Ocelot
- **Порт**: 5000
- **Важно**: Через Gateway можно обращаться ко всем сервисам по упрощённым путям

#### Маршруты через API Gateway:
- **Аутентификация**:
  - `POST /auth/register` → UserService `/api/Auth/register`
  - `POST /auth/login` → UserService `/api/Auth/login`
- **Пользователи**: `/users/*` → UserService `/api/users/*`
- **Продукты**: `/products/*` → ProductService `/api/products/*`
- **Корзины**: `/carts/*` → CartService `/api/carts/*`
- **Заказы**: `/order/*` → OrderService `/api/orders/*`
- **Платежи**: `/payment/*` → PaymentService `/api/payments/*`
- **Адреса доставки**: `/shippingaddress/*` → ShippingAddressService `/api/shipping-addresses/*`

### UserService (Сервис пользователей)
- **URL**: http://localhost:5004/swagger
- **Описание**: Управление пользователями, регистрация, авторизация, JWT токены
- **Порт**: 5004
- **⚠️ Важно**: Этот сервис требует JWT токен для большинства операций

### ProductService (Сервис продуктов)
- **URL**: http://localhost:5002/swagger
- **Описание**: Управление продуктами (товарами)
- **Порт**: 5002

### CartService (Сервис корзины)
- **URL**: http://localhost:5003/swagger
- **Описание**: Управление корзинами покупателей
- **Порт**: 5003

### OrderService (Сервис заказов)
- **URL**: http://localhost:5001/swagger
- **Описание**: Управление заказами
- **Порт**: 5001

### ShippingAddressService (Сервис адресов доставки)
- **URL**: http://localhost:5005/swagger
- **Описание**: Управление адресами доставки
- **Порт**: 5005

### PaymentService (Сервис платежей)
- **URL**: http://localhost:5006/swagger
- **Описание**: Управление платежами
- **Порт**: 5006

## 🚀 Рекомендуемый порядок тестирования

### 1. Регистрация и аутентификация пользователя

#### Вариант A: Через API Gateway (рекомендуется)
1. Откройте http://localhost:5000 в браузере
2. Выполните регистрацию через `POST /auth/register`:
   ```bash
   curl -X POST http://localhost:5000/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test123!"
     }'
   ```
3. Войдите через `POST /auth/login`:
   ```bash
   curl -X POST http://localhost:5000/auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test123!"
     }'
   ```

#### Вариант B: Напрямую через UserService
1. Откройте http://localhost:5004/swagger
2. Найдите endpoint `POST /api/Auth/register`
3. Зарегистрируйте нового пользователя:
   ```json
   {
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
4. Используйте endpoint `POST /api/Auth/login` для получения JWT токена:
   ```json
   {
     "email": "test@example.com",
     "password": "Test123!"
   }
   ```
5. **Скопируйте полученный токен** из ответа

### 2. Настройка авторизации в Swagger
1. В Swagger UI нажмите на кнопку **"Authorize"** (зелёная кнопка с замком вверху справа)
2. В поле введите: `Bearer ваш_токен_здесь`
3. Нажмите "Authorize" и затем "Close"
4. Теперь все запросы будут автоматически включать токен

### 3. Тестирование ProductService
1. Откройте http://localhost:5002/swagger
2. Создайте несколько продуктов через `POST /api/products`
3. Получите список продуктов через `GET /api/products`
4. Получите конкретный продукт через `GET /api/products/{id}`
5. Обновите продукт через `PUT /api/products/{id}`

### 4. Тестирование CartService
1. Откройте http://localhost:5003/swagger
2. Создайте корзину для пользователя
3. Добавьте продукты в корзину
4. Проверьте содержимое корзины
5. Обновите количество товаров
6. Удалите товары из корзины

### 5. Тестирование ShippingAddressService
1. Откройте http://localhost:5005/swagger
2. Создайте адрес доставки для пользователя
3. Получите список адресов
4. Обновите адрес
5. Установите адрес по умолчанию

### 6. Тестирование OrderService
1. Откройте http://localhost:5001/swagger
2. Создайте заказ на основе корзины
3. Получите список заказов пользователя
4. Проверьте детали конкретного заказа
5. Обновите статус заказа

### 7. Тестирование PaymentService
1. Откройте http://localhost:5006/swagger
2. Создайте платёж для заказа
3. Проверьте статус платежа
4. Получите историю платежей

### 8. Тестирование через API Gateway
1. Откройте http://localhost:5000/swagger
2. Проверьте доступные локальные эндпоинты Gateway
3. Попробуйте маршруты, настроенные через Ocelot (если настроены)

## 🔧 Полезные команды

### Проверить статус контейнеров
```bash
docker-compose ps
```

### Посмотреть логи конкретного сервиса
```bash
docker-compose logs userservice -f
docker-compose logs productservice -f
docker-compose logs cartservice -f
# и т.д.
```

### Перезапустить конкретный сервис
```bash
docker-compose restart userservice
```

### Остановить все сервисы
```bash
docker-compose down
```

### Запустить все сервисы заново
```bash
docker-compose up -d
```

### Пересобрать и запустить сервисы
```bash
docker-compose up --build -d
```

## 🗄️ База данных PostgreSQL

- **Host**: localhost
- **Port**: внутренний (не проброшен наружу)
- **Database**: test_loft_shop
- **User**: developer
- **Password**: Devp@ssw0rddB4589

Для подключения к БД изнутри контейнера используйте имя сервиса `postgres`.

## 💡 Советы по тестированию

1. **JWT токены имеют срок действия** (60 минут по умолчанию). Если запросы начнут возвращать 401, получите новый токен через login.

2. **Сохраняйте ID сущностей**: При создании пользователей, продуктов, корзин и т.д., сохраняйте их ID для последующих запросов.

3. **Проверяйте логи**: Если что-то не работает, проверьте логи соответствующего сервиса.

4. **Swagger показывает схемы**: В Swagger UI можно увидеть точные схемы запросов и ответов - используйте кнопку "Schema" рядом с примерами.

5. **Try it out**: В Swagger UI используйте кнопку "Try it out" для редактирования JSON перед отправкой запроса.

## 🐛 Troubleshooting

### Сервис не отвечает
```bash
docker-compose restart <service-name>
docker-compose logs <service-name>
```

### Ошибки базы данных
```bash
docker-compose restart postgres
docker-compose logs postgres
```

### Полный рестарт
```bash
docker-compose down -v
docker-compose up --build -d
```

## ✅ Чек-лист тестирования

- [ ] Регистрация пользователя
- [ ] Вход пользователя и получение JWT токена
- [ ] Создание продукта
- [ ] Получение списка продуктов
- [ ] Создание корзины
- [ ] Добавление товаров в корзину
- [ ] Создание адреса доставки
- [ ] Создание заказа
- [ ] Создание платежа
- [ ] Проверка работы всех CRUD операций

---

**Приятного тестирования!** 🎉

Если возникнут вопросы или проблемы, проверьте логи соответствующего сервиса.
