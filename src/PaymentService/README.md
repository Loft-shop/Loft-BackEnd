# Payment Service

Сервис для управления платежами с поддержкой трех методов оплаты.

## Поддерживаемые методы оплаты

### 1. **STRIPE** (Онлайн платежи)
- ✅ **Real Stripe Integration** - полная интеграция с Stripe API (Test Mode)
- Автоматическое создание PaymentIntent
- Поддержка тестовых карт Stripe
- Статусы: `REQUIRES_CONFIRMATION` → `COMPLETED`
- TransactionId формат: `pi_xxx` (Stripe PaymentIntent ID)

### 2. **CREDIT_CARD** (Прямая оплата картой)
- Мок-версия прямой оплаты банковской картой
- Без использования сторонних сервисов
- Статусы: `REQUIRES_CONFIRMATION` → `COMPLETED`
- TransactionId формат: `card_mock_xxxxx`

### 3. **CASH_ON_DELIVERY** (Оплата наличными при доставке)
- Оплата курьеру при получении товара
- Статусы: `PENDING` → `COMPLETED` (после подтверждения доставки)
- TransactionId формат: `cash_mock_xxxxx`

## Архитектура

```
┌─────────────────────────────────┐
│    PaymentsController           │
│    (REST API)                   │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│    PaymentService               │
│    (Бизнес-логика)              │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  PaymentProviderFactory         │
│  (Выбор провайдера)             │
└────────────┬────────────────────┘
             │
     ┌───────┼───────┐
     │       │       │
     ▼       ▼       ▼
  Stripe  Card    Cash
   Mock    Mock    Mock
```

## API Endpoints

### Создать платеж
```http
POST /api/payments
Content-Type: application/json

{
  "orderId": 123,
  "amount": 99.99,
  "method": 2  // 0=CREDIT_CARD, 1=CASH_ON_DELIVERY, 2=STRIPE
}
```

**Ответ:**
```json
{
  "id": 1,
  "orderId": 123,
  "amount": 99.99,
  "method": 2,
  "status": 1,
  "paymentDate": "2025-11-19T10:00:00Z",
  "transactionId": "stripe_mock_abc123..."
}
```

### Подтвердить платеж
```http
POST /api/payments/{id}/confirm
```

### Получить платеж
```http
GET /api/payments/{id}
GET /api/payments/order/{orderId}
```

### Вернуть платеж
```http
POST /api/payments/{id}/refund
```

### Получить методы оплаты
```http
GET /api/payments/methods
```

## Enum значения

### PaymentMethod
- `0` = CREDIT_CARD
- `1` = CASH_ON_DELIVERY
- `2` = STRIPE

### PaymentStatus
- `0` = PENDING
- `1` = REQUIRES_CONFIRMATION
- `2` = PROCESSING
- `3` = COMPLETED
- `4` = FAILED
- `5` = REFUNDED
- `6` = PARTIALLY_REFUNDED

## Установка и запуск

### Настройка Stripe Test Mode:

1. Получите тестовые ключи на https://dashboard.stripe.com (Test Mode)
2. Настройте конфигурацию:

```bash
# Через переменные окружения (рекомендуется)
export Stripe__SecretKey="sk_test_ваш_ключ"
export Stripe__PublishableKey="pk_test_ваш_ключ"

# Или в appsettings.Development.json
{
  "Stripe": {
    "SecretKey": "sk_test_ваш_ключ",
    "PublishableKey": "pk_test_ваш_ключ"
  }
}
```

3. Запустите:
```bash
cd src/PaymentService
dotnet restore
dotnet ef database update
dotnet run
```

📖 **Подробная инструкция**: см. [STRIPE_SETUP.md](STRIPE_SETUP.md)
cd src/PaymentService
dotnet restore
dotnet ef database update
dotnet run
```

Сервис будет доступен на `http://localhost:5005`
Swagger UI: `http://localhost:5005/swagger`

## Использование

### Пример 1: Оплата через Stripe
```bash
curl -X POST http://localhost:5005/api/payments \
  -H "Content-Type: application/json" \
  -d '{"orderId": 1, "amount": 99.99, "method": 2}'
```

### Пример 2: Подтверждение платежа
```bash
curl -X POST http://localhost:5005/api/payments/1/confirm
```

### Пример 3: Возврат платежа
```bash
curl -X POST http://localhost:5005/api/payments/1/refund
```

## Интеграция с Loft.Common

Использует общие типы:
- `Loft.Common.Enums.PaymentMethod`
- `Loft.Common.Enums.PaymentStatus`
- `Loft.Common.DTOs.PaymentDTO`
- `Loft.Common.DTOs.CreatePaymentDTO`

## Логирование

Все операции логируются с префиксами:
- `[MOCK STRIPE]` - операции Stripe
- `[MOCK CREDIT CARD]` - операции с картами
- `[MOCK CASH ON DELIVERY]` - операции с наличными

## База данных

Таблица `Payments`:
- `Id` (bigint) - уникальный идентификатор
- `OrderId` (bigint) - ID заказа
- `Amount` (decimal) - сумма платежа
- `Method` (int) - метод оплаты
- `Status` (int) - статус платежа
- `PaymentDate` (timestamp) - дата платежа
- `TransactionId` (text) - ID транзакции в платежной системе

