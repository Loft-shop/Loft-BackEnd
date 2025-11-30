# Быстрый старт - Тестирование Stripe Integration

Ваш Stripe Test API Key уже настроен в `appsettings.Development.json`.

## Запуск сервиса

```bash
cd src/PaymentService
dotnet run
```

Вы должны увидеть:
```
=== Payment Service Started ===
[REAL STRIPE] Initialized with API key: sk_test_51...
Supported payment methods:
  - STRIPE (Real - Test Mode)
  - CREDIT_CARD (Mock)
  - CASH_ON_DELIVERY (Mock)
```

## Тестирование через HTTP файл

Откройте `PaymentService.http` в Rider и выполните запросы.

### Пример 1: Создать платеж £5.00
```http
POST http://localhost:5005/api/payments
Content-Type: application/json

{
  "orderId": 1,
  "amount": 5.00,
  "method": 2
}
```

**Ожидаемый ответ:**
```json
{
  "id": 1,
  "orderId": 1,
  "amount": 5.00,
  "method": 2,
  "status": 1,
  "paymentDate": "2025-11-19T...",
  "transactionId": "pi_3Abc..." // ← Реальный Stripe PaymentIntent ID!
}
```

### Пример 2: Проверить в Stripe Dashboard

1. Откройте https://dashboard.stripe.com/test/payments
2. Найдите ваш PaymentIntent по `transactionId` из ответа
3. Увидите детали платежа: amount=500 (£5.00), currency=gbp, status=requires_confirmation

## Тестовые карты Stripe

Используйте для confirm:
- **Visa успешная**: `4242 4242 4242 4242`
- **Требует 3D Secure**: `4000 0027 6000 3184`
- **Отклонена**: `4000 0000 0000 9995`

## Валюта

- Сейчас настроено: **GBP** (British Pounds)
- Amount в запросе: фунты (например, `5.00`)
- Stripe получает: пенсы (например, `500`)

Чтобы переключить на USD:
1. Откройте `RealStripeProvider.cs`
2. Измените `Currency = "gbp"` на `Currency = "usd"`

## Логи

В консоли увидите:
```
[REAL STRIPE] Created PaymentIntent pi_3Abc... for order 1, amount 500 pence, status: requires_confirmation
```

## Troubleshooting

### Ошибка "Invalid API Key"
- Проверьте, что ключ правильно скопирован в `appsettings.Development.json`
- Убедитесь, что Stripe Dashboard в Test Mode

### Платеж не создается
- Проверьте логи в консоли
- Убедитесь, что БД доступна
- Проверьте миграции: `dotnet ef database update`

