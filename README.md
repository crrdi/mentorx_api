# MentorX API

.NET 8 tabanlı, çok katmanlı mimariye sahip MentorX API projesi.

## Teknoloji Stack

- **.NET 8** - Framework
- **ASP.NET Core Web API** - API framework
- **Entity Framework Core 8** - ORM (PostgreSQL provider)
- **Supabase** - Database ve Auth
- **Swagger/OpenAPI** - API documentation
- **AutoMapper** - Object mapping
- **FluentValidation** - Request validation
- **Serilog** - Logging

## Proje Yapısı

```
MentorX.API/                    # Presentation Layer
├── Controllers/                # API Controllers
├── Middleware/                 # Custom middleware
├── Filters/                    # Action filters
└── Extensions/                 # Startup extensions

MentorX.Application/            # Application Layer
├── Services/                   # Business logic services
├── DTOs/                       # Data Transfer Objects
├── Mappings/                   # AutoMapper profiles
├── Validators/                 # FluentValidation validators
└── Interfaces/                 # Service interfaces

MentorX.Domain/                 # Domain Layer
├── Entities/                   # Domain entities
├── Enums/                      # Domain enums
└── Interfaces/                 # Repository interfaces

MentorX.Infrastructure/         # Infrastructure Layer
├── Data/                       # Data access
│   ├── Repositories/          # Repository implementations
│   ├── DbContext/             # EF Core DbContext
│   └── Migrations/             # Database migrations
└── Services/                   # External services (Supabase)
```

## Kurulum

### 1. Supabase Projesi Oluşturma

#### Yöntem 1: Web Dashboard (Önerilen)

1. [Supabase Dashboard](https://supabase.com/dashboard) adresine gidin
2. "New Project" butonuna tıklayın
3. Proje bilgilerini doldurun:
   - **Name**: MentorX
   - **Database Password**: Güçlü bir şifre belirleyin (kaydedin!)
   - **Region**: Size en yakın bölgeyi seçin
4. Proje oluşturulduktan sonra:
   - **Settings** > **API** sayfasına gidin
   - **Project URL** ve **anon public** key'i kopyalayın
   - **Settings** > **API** > **Service Role** key'i kopyalayın (gizli tutun!)

#### Yöntem 2: Management API

```bash
# Access token alın: https://supabase.com/dashboard/account/tokens
export SUPABASE_ACCESS_TOKEN="your-access-token"

# Organizasyon ID'nizi alın
curl -H "Authorization: Bearer $SUPABASE_ACCESS_TOKEN" \
  https://api.supabase.com/v1/organizations

# Proje oluşturun (org-id'yi değiştirin)
curl -X POST https://api.supabase.com/v1/projects \
  -H "Authorization: Bearer $SUPABASE_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "organization_id": "<org-id>",
    "name": "MentorX",
    "region": "us-east-1",
    "db_pass": "<your-secure-password>"
  }'
```

### 2. Database Connection String

Supabase Dashboard'dan connection string'i alın:

1. **Settings** > **Database** sayfasına gidin
2. **Connection string** bölümünden **URI** formatını kopyalayın
3. Şifreyi değiştirin: `[YOUR-PASSWORD]` → gerçek şifreniz

Format:
```
postgresql://postgres:[YOUR-PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres
```

### 3. Konfigürasyon

`appsettings.json` dosyasını güncelleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres:[YOUR-PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres"
  },
  "Supabase": {
    "Url": "https://[PROJECT-REF].supabase.co",
    "ServiceRoleKey": "your-service-role-key-here"
  }
}
```

### 4. Database Migration

```bash
# Migration'ları uygula
dotnet ef database update --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj
```

### 5. Uygulamayı Çalıştırma

```bash
cd MentorX.API
dotnet run
```

API Swagger UI'da açılacak: `http://localhost:5000` veya `https://localhost:5001`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Kullanıcı kaydı
- `POST /api/auth/login` - Giriş
- `POST /api/auth/google` - Google OAuth (yakında)
- `POST /api/auth/apple` - Apple OAuth (yakında)

### Users
- `GET /api/users/me` - Mevcut kullanıcı bilgileri
- `PUT /api/users/me` - Kullanıcı bilgilerini güncelle

### Mentors
- `GET /api/mentors` - Mentor listesi
- `GET /api/mentors/{id}` - Mentor detayı
- `POST /api/mentors` - Mentor oluştur
- `PUT /api/mentors/{id}` - Mentor güncelle
- `POST /api/mentors/{id}/follow` - Mentor takip et
- `DELETE /api/mentors/{id}/follow` - Mentor takibi bırak
- `GET /api/mentors/{id}/replies` - Mentor yanıtları

### Insights
- `GET /api/insights` - Insight listesi
- `GET /api/insights/{id}` - Insight detayı
- `POST /api/insights` - Insight oluştur
- `GET /api/feed` - Kişisel feed

### Comments
- `GET /api/insights/{id}/comments` - Yorum listesi
- `POST /api/insights/{id}/comments` - Yorum ekle

### Conversations & Messages
- `GET /api/conversations` - Konuşma listesi
- `POST /api/conversations` - Konuşma oluştur
- `GET /api/conversations/{id}/messages` - Mesaj listesi
- `POST /api/conversations/{id}/messages` - Mesaj gönder

### Credits
- `GET /api/credits/packages` - Kredi paketleri
- `GET /api/credits/balance` - Kredi bakiyesi
- `POST /api/credits/purchase` - Kredi satın al

### Tags
- `GET /api/tags/popular` - Popüler tag'ler

## Development

### Migration Oluşturma

```bash
dotnet ef migrations add MigrationName --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj
```

### Migration Uygulama

```bash
dotnet ef database update --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj
```

## Notlar

- Supabase Auth ile kullanıcı kaydı yapıldığında otomatik olarak `Users` ve `Actors` tablolarına kayıt eklenir
- `ExpertisePrompt` alanı asla API'de expose edilmez (güvenlik)
- Soft delete pattern kullanılıyor (`deletedAt` field)
- Counter cache'ler otomatik güncellenir (followerCount, insightCount, vb.)

## Güvenlik

- Supabase Service Role Key'i asla client-side'da kullanmayın
- Production'da connection string'i environment variable olarak saklayın
- Row Level Security (RLS) politikaları Supabase dashboard'dan yapılandırılmalı
