# MediaService

Микросервис для управления медиа-файлами (изображениями, аватарами пользователей, изображениями продуктов).

## Возможности

- ✅ Загрузка изображений (JPG, PNG, GIF, WebP)
- ✅ Автоматическое создание миниатюр (thumbnails)
- ✅ Валидация изображений
- ✅ Категоризация файлов (avatars, products, general)
- ✅ Скачивание и просмотр файлов
- ✅ Удаление файлов
- ✅ Список всех файлов по категориям
- ✅ JWT аутентификация
- ✅ Swagger документация

## Технологии

- .NET 8.0
- ASP.NET Core Web API
- SixLabors.ImageSharp (обработка изображений)
- JWT Bearer Authentication

## API Endpoints

### 1. Загрузка файлов

#### Загрузить файл в общую категорию
```http
POST /api/Media/upload?category=general
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

#### Загрузить аватар пользователя
```http
POST /api/Media/upload/avatar
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

#### Загрузить изображение продукта
```http
POST /api/Media/upload/product
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

**Response:**
```json
{
  "fileName": "unique-guid.jpg",
  "fileUrl": "/media/avatars/unique-guid.jpg",
  "thumbnailUrl": "/media/avatars/thumbnails/thumb_unique-guid.jpg",
  "fileSize": 1024000,
  "contentType": "image/jpeg",
  "uploadedAt": "2025-11-17T10:00:00Z"
}
```

### 2. Просмотр и скачивание

#### Просмотр файла (inline)
```http
GET /api/Media/view/{fileName}
```

#### Скачивание файла
```http
GET /api/Media/download/{fileName}
```

### 3. Управление файлами

#### Получить список всех файлов
```http
GET /api/Media/files
Authorization: Bearer {token}
```

#### Получить список файлов по категории
```http
GET /api/Media/files?category=avatars
Authorization: Bearer {token}
```

#### Удалить файл
```http
DELETE /api/Media/delete/{fileName}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "File deleted successfully"
}
```

### 4. Health Check
```http
GET /api/Media/health
```

## Конфигурация

### appsettings.json

```json
{
  "MediaSettings": {
    "MaxFileSizeMB": 10,
    "AllowedImageExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    "AllowedVideoExtensions": [".mp4", ".avi", ".mov"],
    "AllowedDocumentExtensions": [".pdf", ".doc", ".docx"],
    "ThumbnailWidth": 200,
    "ThumbnailHeight": 200,
    "UploadPath": "uploads"
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "LoftUserService",
    "Audience": "LoftUsers",
    "ExpireMinutes": 60
  }
}
```

## Docker

### Запуск через Docker Compose
```bash
docker-compose up mediaservice
```

Сервис будет доступен на порту **5008**.

### Переменные окружения
```yaml
ASPNETCORE_URLS: http://0.0.0.0:8080
Jwt__Key: your-secret-key
Jwt__Issuer: LoftUserService
Jwt__Audience: LoftUsers
```

## Структура файлов

```
uploads/
├── avatars/
│   ├── image1.jpg
│   └── thumbnails/
│       └── thumb_image1.jpg
├── products/
│   ├── product1.jpg
│   └── thumbnails/
│       └── thumb_product1.jpg
└── general/
    └── file1.jpg
```

## Примеры использования

### С помощью curl

```bash
# Загрузить аватар
curl -X POST "http://localhost:5008/api/Media/upload/avatar" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@avatar.jpg"

# Просмотреть файл
curl "http://localhost:5008/api/Media/view/unique-guid.jpg" \
  --output image.jpg

# Получить список файлов
curl -X GET "http://localhost:5008/api/Media/files?category=avatars" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Удалить файл
curl -X DELETE "http://localhost:5008/api/Media/delete/unique-guid.jpg" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Через API Gateway

Все запросы проксируются через API Gateway:
```
http://localhost:5000/api/media/* -> http://mediaservice:8080/api/media/*
```

## Интеграция с другими сервисами

### UserService
Используйте MediaService для загрузки аватаров пользователей:
```csharp
// После загрузки аватара получите URL
var avatarUrl = uploadResponse.FileUrl;
// Сохраните URL в профиле пользователя
user.AvatarUrl = avatarUrl;
```

### ProductService
Используйте MediaService для загрузки изображений продуктов:
```csharp
// Загрузите изображение продукта
var productImageUrl = uploadResponse.FileUrl;
var thumbnailUrl = uploadResponse.ThumbnailUrl;
// Сохраните URL в продукте
product.ImageUrl = productImageUrl;
product.ThumbnailUrl = thumbnailUrl;
```

## Безопасность

- ✅ Проверка типов файлов (только разрешенные расширения)
- ✅ Проверка размера файлов (максимум 10 МБ по умолчанию)
- ✅ Валидация содержимого изображений
- ✅ JWT аутентификация для загрузки и удаления
- ✅ Публичный доступ для просмотра файлов

## Разработка

### Запуск локально
```bash
cd src/MediaService
dotnet restore
dotnet run
```

### Запуск тестов
```bash
dotnet test
```

## TODO / Будущие улучшения

- [ ] Поддержка видео файлов
- [ ] Поддержка документов (PDF, DOC)
- [ ] Интеграция с облачными хранилищами (S3, Azure Blob)
- [ ] Водяные знаки на изображениях
- [ ] Множественная загрузка файлов
- [ ] Оптимизация изображений (сжатие)
- [ ] CDN интеграция

