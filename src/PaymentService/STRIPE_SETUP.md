# Настройка Stripe Test Mode для PaymentService

Этот гайд покажет как настроить реальную интеграцию с Stripe в тестовом режиме.

## Шаг 1: Получение тестовых ключей Stripe

1. Зайдите на https://dashboard.stripe.com/register
2. Создайте аккаунт или войдите
3. В дашборде переключитесь в **Test Mode** (переключатель справа вверху)
4. Перейдите в **Developers** → **API keys**
5. Скопируйте:
   - **Publishable key** (начинается с `pk_test_`)
   - **Secret key** (начинается с `sk_test_`)

## Шаг 2: Настройка appsettings.json

Откройте `src/PaymentService/appsettings.Development.json` и добавьте ваши ключи:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_ваш_секретный_ключ_здесь",
    "PublishableKey": "pk_test_ваш_публичный_ключ_здесь"
  }
}
```

**⚠️ ВАЖНО:** Никогда не коммитьте реальные ключи в Git!

## Шаг 3: Использование переменных окружения (рекомендуется)

Вместо хардкода в appsettings, используйте переменные окружения:

### macOS/Linux:
```bash
export Stripe__SecretKey="sk_test_ваш_ключ"
export Stripe__PublishableKey="pk_test_ваш_ключ"
```

### Windows (PowerShell):
```powershell
$env:Stripe__SecretKey="sk_test_ваш_ключ"
$env:Stripe__PublishableKey="pk_test_ваш_ключ"
```

### Docker Compose:
```yaml
services:
  payment-service:
    environment:
      - Stripe__SecretKey=sk_test_ваш_ключ
      - Stripe__PublishableKey=pk_test_ваш_ключ
```

## Шаг 4: Тестовые карты Stripe

В Test Mode используйте эти карты для тестирования:

### Успешные платежи:
- **Visa**: `4242 4242 4242 4242`
- **Visa (debit)**: `4000 0566 5566 5556`
- **Mastercard**: `5555 5555 5555 4444`
- **American Express**: `3782 822463 10005`

### Специальные сценарии:
- **Отклонен (недостаточно средств)**: `4000 0000 0000 9995`
- **Отклонен (украденная карта)**: `4000 0000 0000 9979`
- **Требует аутентификации**: `4000 0027 6000 3184`

**Любые данные для:**
- CVC: любые 3 цифры (например, `123`)
- Дата истечения: любая будущая дата (например, `12/25`)
- Почтовый индекс: любой (например, `12345`)

## Шаг 5: Тестирование

### Запуск сервиса:
```bash
cd src/PaymentService
dotnet run
```

### Проверка в логах:
При старте вы увидите:
```
[PaymentService] Using REAL Stripe provider (Test Mode)
[REAL STRIPE] Initialized with API key: sk_test_51...
```

### Тестовый запрос:
```bash
curl -X POST http://localhost:5005/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": 1,
    "amount": 99.99,
    "method": 2
  }'
```

### Ожидаемый ответ:
```json
{
  "id": 1,
  "orderId": 1,
  "amount": 99.99,
  "method": 2,
  "status": 1,
  "paymentDate": "2025-11-19T...",
  "transactionId": "pi_3Abc123..."
}
```

Где `transactionId` начинается с `pi_` - это реальный Stripe PaymentIntent ID!

## Шаг 6: Проверка в Stripe Dashboard

1. Зайдите в https://dashboard.stripe.com/test/payments
2. Найдите ваш платеж по ID из `transactionId`
3. Увидите все детали платежа в Stripe

## Шаг 7: Webhooks (опционально)

Для получения событий от Stripe (успех платежа, возврат и т.д.):

1. Установите Stripe CLI: https://stripe.com/docs/stripe-cli
2. Запустите webhook forwarding:
   ```bash
   stripe listen --forward-to localhost:5005/api/webhooks/stripe
   ```
3. Скопируйте webhook secret и добавьте в конфиг:
   ```json
   {
     "Stripe": {
       "WebhookSecret": "whsec_..."
     }
   }
   ```

## Troubleshooting

### Ошибка "Invalid API Key"
- Проверьте, что ключ начинается с `sk_test_`
- Убедитесь, что в Dashboard включен Test Mode

### Ошибка "Stripe:SecretKey is not configured"
- Добавьте ключ в appsettings.json или переменные окружения
- Проверьте синтаксис (двойное подчеркивание для env vars)

### Платеж не подтверждается
- В тестовом режиме используйте тестовые карты
- Проверьте, что карта не требует 3D Secure

## Production Mode

⚠️ **НЕ ИСПОЛЬЗУЙТЕ TEST KEYS В PRODUCTION!**

Для production:
1. Переключите Stripe Dashboard в Live Mode
2. Получите live ключи (`sk_live_` и `pk_live_`)
3. Настройте webhook endpoints
4. Включите дополнительную безопасность

