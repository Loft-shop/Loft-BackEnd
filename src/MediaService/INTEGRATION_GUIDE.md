# MediaService - Инструкция по интеграции

## Что было создано

Создан новый микросервис **MediaService** для управления медиа-файлами (изображениями, аватарами, файлами продуктов).

### Структура проекта

```
src/MediaService/
├── Controllers/
│   └── MediaController.cs          # API endpoints для работы с медиа
├── Services/
│   ├── IMediaService.cs            # Интерфейс сервиса
│   ├── MediaStorageService.cs      # Реализация хранения файлов
│   ├── IImageProcessingService.cs  # Интерфейс обработки изображений
│   └── ImageProcessingService.cs   # Обработка и создание миниатюр
├── Entities/
│   └── MediaFile.cs                # Entity модель файла
├── Mappings/
│   └── MediaProfile.cs             # AutoMapper профиль
├── Program.cs                      # Точка входа
├── Dockerfile                      # Docker конфигурация
├── MediaService.csproj             # Проект файл
├── appsettings.json                # Настройки
└── README.md                       # Документация

src/Common/Loft.Common/DTOs/
├── MediaFileDTO.cs                 # DTO для файла
├── UploadResponseDTO.cs            # DTO для ответа загрузки
└── DeleteResponseDTO.cs            # DTO для ответа удаления
```

## Функционал

### ✅ Реализовано

1. **Загрузка файлов** (изображения: JPG, PNG, GIF, WebP)
   - Общая загрузка файлов
   - Специальный endpoint для аватаров
   - Специальный endpoint для изображений продуктов

2. **Обработка изображений**
   - Валидация изображений
   - Автоматическое создание миниатюр (thumbnails)
   - Определение MIME типов

3. **Управление файлами**
   - Просмотр файлов
   - Скачивание файлов
   - Удаление файлов
   - Список файлов по категориям

4. **Безопасность**
   - JWT аутентификация для загрузки/удаления
   - Валидация размера файлов (макс. 10 МБ)
   - Валидация типов файлов
   - Публичный доступ для просмотра

5. **Интеграция**
   - Добавлен в docker-compose.yaml
   - Настроен маршрутизация в API Gateway (ocelot.json)
   - Добавлен в solution файл

## API Endpoints

### 1. Загрузка файлов (требует авторизацию)

```http
POST /api/Media/upload?category=general
Authorization: Bearer {token}
Content-Type: multipart/form-data

POST /api/Media/upload/avatar
Authorization: Bearer {token}

POST /api/Media/upload/product
Authorization: Bearer {token}
```

**Response:**
```json
{
  "id": 0,
  "fileName": "unique-guid.jpg",
  "fileUrl": "/media/avatars/unique-guid.jpg",
  "thumbnailUrl": "/media/avatars/thumbnails/thumb_unique-guid.jpg",
  "fileSize": 1024000,
  "contentType": "image/jpeg",
  "category": "avatars",
  "uploadedAt": "2025-11-17T10:00:00Z"
}
```

### 2. Просмотр и скачивание (публичный доступ)

```http
GET /api/Media/view/{fileName}
GET /api/Media/download/{fileName}
```

### 3. Управление (требует авторизацию)

```http
GET /api/Media/files?category=avatars
Authorization: Bearer {token}

DELETE /api/Media/delete/{fileName}
Authorization: Bearer {token}
```

### 4. Health check

```http
GET /api/Media/health
```

## Как использовать в других сервисах

### В UserService - для загрузки аватаров

```csharp
// 1. Пользователь загружает аватар через MediaService
// POST /api/media/upload/avatar

// 2. MediaService возвращает URL
var response = new UploadResponseDTO {
    FileUrl = "/media/avatars/unique-guid.jpg",
    ThumbnailUrl = "/media/avatars/thumbnails/thumb_unique-guid.jpg"
};

// 3. Сохраняем URL в профиле пользователя
user.AvatarUrl = response.FileUrl;
await _userRepository.UpdateAsync(user);
```

### В ProductService - для изображений продуктов

```csharp
// 1. Продавец загружает изображение через MediaService
// POST /api/media/upload/product

// 2. Сохраняем URL в продукте
product.ImageUrl = response.FileUrl;
product.ThumbnailUrl = response.ThumbnailUrl;
await _productRepository.UpdateAsync(product);
```

## Конфигурация

### Настройки в appsettings.json

```json
{
  "MediaSettings": {
    "MaxFileSizeMB": 10,
    "AllowedImageExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    "ThumbnailWidth": 200,
    "ThumbnailHeight": 200,
    "UploadPath": "uploads"
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "LoftUserService",
    "Audience": "LoftUsers"
  }
}
```

### Docker Compose

Сервис добавлен в `compose.yaml`:
- Порт: **5008**
- URL: `http://localhost:5008`
- URL в Docker сети: `http://mediaservice:8080`

### API Gateway

Маршруты добавлены в `ocelot.json`:
```
/api/media/* -> http://mediaservice:8080/api/media/*
```

## Запуск

### Локально (для разработки)

```bash
cd src/MediaService
dotnet restore
dotnet run
# Сервис запустится на http://localhost:5008
# Swagger: http://localhost:5008/swagger
```

### Через Docker Compose

```bash
# Запустить только MediaService
docker-compose up mediaservice

# Запустить все сервисы
docker-compose up -d

# Пересобрать MediaService
docker-compose build mediaservice
docker-compose up -d mediaservice
```

## Тестирование

### С помощью curl

```bash
# 1. Получить JWT токен (через UserService)
TOKEN="your-jwt-token"

# 2. Загрузить аватар
curl -X POST "http://localhost:5008/api/Media/upload/avatar" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@avatar.jpg"

# 3. Просмотреть файл
curl "http://localhost:5008/api/Media/view/filename.jpg" \
  --output downloaded.jpg

# 4. Получить список файлов
curl -X GET "http://localhost:5008/api/Media/files?category=avatars" \
  -H "Authorization: Bearer $TOKEN"

# 5. Удалить файл
curl -X DELETE "http://localhost:5008/api/Media/delete/filename.jpg" \
  -H "Authorization: Bearer $TOKEN"
```

### Через API Gateway (Production)

```bash
# Все запросы идут через API Gateway на порт 5000
curl -X POST "http://localhost:5000/api/media/upload/avatar" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@avatar.jpg"
```

### HTTP файл (Rider/VS Code)

Используйте файл `MediaService.http` для тестирования в IDE.

## Архитектура

### Хранение файлов

Файлы хранятся локально в структуре:
```
uploads/
├── avatars/
│   ├── guid1.jpg
│   ├── guid2.png
│   └── thumbnails/
│       ├── thumb_guid1.jpg
│       └── thumb_guid2.png
├── products/
│   └── thumbnails/
└── general/
```

### Entity модель (MediaFile)

```csharp
public class MediaFile
{
    public long Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; }
    public string Category { get; set; }
    public long? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // ... другие поля
}
```

### DTO (в Loft.Common)

- **MediaFileDTO** - полная информация о файле
- **UploadResponseDTO** - ответ при загрузке
- **DeleteResponseDTO** - ответ при удалении

### Mapping (AutoMapper)

Маппинги настроены в `MediaProfile.cs`:
- MediaFile ↔ MediaFileDTO
- MediaFile → UploadResponseDTO

## Следующие шаги

### Рекомендации по улучшению

1. **База данных** (опционально)
   - Добавить DbContext для хранения метаданных файлов
   - Создать миграции
   - Связать файлы с пользователями/продуктами

2. **Интеграция с UserService**
   ```csharp
   // В UserController добавить метод обновления аватара
   [HttpPut("avatar")]
   public async Task<IActionResult> UpdateAvatar(string mediaFileUrl)
   {
       var user = await GetCurrentUser();
       user.AvatarUrl = mediaFileUrl;
       await _userService.UpdateAsync(user);
       return Ok();
   }
   ```

3. **Интеграция с ProductService**
   ```csharp
   // В ProductController добавить загрузку изображений
   [HttpPost("{id}/images")]
   public async Task<IActionResult> AddProductImage(int id, string mediaFileUrl)
   {
       var product = await _productService.GetByIdAsync(id);
       product.ImageUrl = mediaFileUrl;
       await _productService.UpdateAsync(product);
       return Ok();
   }
   ```

4. **Облачное хранилище** (для production)
   - AWS S3
   - Azure Blob Storage
   - Google Cloud Storage

5. **Дополнительные функции**
   - Водяные знаки
   - Оптимизация/сжатие изображений
   - Поддержка видео
   - CDN интеграция

## Зависимости

- ✅ .NET 8.0
- ✅ AutoMapper 12.0.1
- ✅ SixLabors.ImageSharp 3.1.7
- ✅ JWT Bearer Authentication
- ✅ Swagger/OpenAPI
- ✅ Loft.Common (проект)

## Порты

- **Local**: 5008 (HTTP)
- **Docker**: 8080 (внутри контейнера)
- **API Gateway**: 5000 → mediaservice:8080

## Статус

✅ MediaService создан и готов к использованию!
✅ Интегрирован с API Gateway
✅ Добавлен в Docker Compose
✅ DTO вынесены в Common
✅ Entity модели созданы
✅ Mappings настроены
✅ Документация готова

Сервис готов к тестированию и интеграции с UserService и ProductService! 🚀

