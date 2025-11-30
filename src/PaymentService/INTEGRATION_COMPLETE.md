# ✅ PaymentService - Stripe Integration Complete

## Что сделано

### 1. ✅ Stripe API Key настроен
- Ваш тестовый ключ добавлен в `appsettings.Development.json`
- Ключ: `sk_test_51S4OJBJ4aECqsnGY...`
- **⚠️ Этот ключ для TEST MODE** - безопасно использовать для разработки

### 2. ✅ RealStripeProvider обновлен
- Используется ваш пример кода Stripe
- Валюта: **GBP** (British Pounds)
- Amount автоматически конвертируется в пенсы (×100)
- PaymentMethod: `pm_card_visa` (тестовая карта)
- PaymentMethodTypes: `["card"]`

### 3. ✅ Миграции обновлены
- Добавлена колонка `TransactionId` в таблицу `Payments`
- SQL скрипт сгенерирован: `migrations_idempotent.sql`
- Готов к применению

### 4. ✅ Проект собирается без ошибок
- Все зависимости установлены
- Stripe.net v50.0.0 подключен

## Как запустить

```bash
cd src/PaymentService

# Применить миграции
dotnet ef database update

# Запустить сервис
dotnet run
```

## Что увидите при старте

```
=== Payment Service Started ===
[REAL STRIPE] Initialized with API key: sk_test_51S4OJBJ4aECqsnGY...
Supported payment methods:
  - STRIPE (Real - Test Mode)
  - CREDIT_CARD (Mock)
  - CASH_ON_DELIVERY (Mock)
================================
```

## Тестовый запрос

```bash
curl -X POST http://localhost:5005/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": 1,
    "amount": 5.00,
    "method": 2
  }'
```

**Ожидаемый результат:**
```json
{
  "id": 1,
  "orderId": 1,
  "amount": 5.00,
  "method": 2,
  "status": 1,
  "paymentDate": "2025-11-19T...",
  "transactionId": "pi_3PQz..." // ← Реальный Stripe PaymentIntent!
}
```

## Проверка в Stripe Dashboard

1. Откройте: https://dashboard.stripe.com/test/payments
2. Найдите ваш платеж по `transactionId`
3. Увидите:
   - Amount: 500 (£5.00 в пенсах)
   - Currency: GBP
   - Status: requires_confirmation
   - Payment Method: Visa (pm_card_visa)

## Файлы для ознакомления

- 📖 **QUICK_START.md** - Быстрый старт и примеры
- 📖 **STRIPE_SETUP.md** - Подробная настройка Stripe
- 📖 **README.md** - Общая документация
- 🧪 **PaymentService.http** - HTTP запросы для тестирования

## Следующие шаги (опционально)

1. **Получить Publishable Key**
   - Зайдите в https://dashboard.stripe.com/test/apikeys
   - Скопируйте `pk_test_...`
   - Добавьте в `appsettings.Development.json`

2. **Настроить Webhooks** (для получения событий от Stripe)
   - Установите Stripe CLI: https://stripe.com/docs/stripe-cli
   - Запустите: `stripe listen --forward-to localhost:5005/webhook`

3. **Переключить на USD**
   - В `RealStripeProvider.cs` измените `Currency = "gbp"` → `"usd"`

4. **Production Mode**
   - Получите live ключи (`sk_live_...`)
   - Добавьте в production конфигурацию
   - **НЕ КОММИТЬТЕ** live ключи в Git!

## Готово! 🎉

Ваш PaymentService готов к работе с реальным Stripe API в тестовом режиме!

