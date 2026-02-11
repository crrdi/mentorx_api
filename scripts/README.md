# Kurulum Script'leri

Bu klasörde Supabase projesi kurulumu için hazır script'ler bulunmaktadır.

## Script'ler

### 1. create-supabase-project.sh
Supabase Management API kullanarak yeni bir proje oluşturur.

**Kullanım:**
```bash
export SUPABASE_ACCESS_TOKEN="your-access-token"
./scripts/create-supabase-project.sh
```

**Gereksinimler:**
- `SUPABASE_ACCESS_TOKEN` environment variable
- `jq` (JSON parser)
- `curl`
- `openssl`

**Access Token Alma:**
1. https://supabase.com/dashboard/account/tokens adresine gidin
2. "Generate new token" butonuna tıklayın
3. Token'ı kopyalayın ve export edin

### 2. setup-config.sh
appsettings.json dosyasını otomatik olarak günceller.

**Kullanım:**
```bash
./scripts/setup-config.sh [PROJECT_REF] [DB_PASSWORD] [SERVICE_ROLE_KEY]
```

**Örnek:**
```bash
./scripts/setup-config.sh abcdefghijklmnop MySecurePassword123 eyJhbGci...
```

### 3. setup-complete.sh
Tüm kurulum adımlarını otomatik olarak yapar.

**Kullanım:**
```bash
export SUPABASE_ACCESS_TOKEN="your-access-token"
./scripts/setup-complete.sh
```

## SQL Script'leri

### 01-rls-policies.sql
Row Level Security (RLS) politikalarını ekler. Supabase SQL Editor'de çalıştırın.

### 02-triggers.sql
Database trigger'larını ekler (counter cache güncellemeleri, yeni kullanıcı oluşturma, vb.)

### 03-seed-data.sql
Başlangıç verilerini ekler (MentorRole, CreditPackages)

## Manuel Kurulum

Script'leri kullanmak istemiyorsanız, `SETUP_SUPABASE.md` dosyasındaki adımları takip edebilirsiniz.

## Sorun Giderme

### jq bulunamadı hatası
```bash
# macOS
brew install jq

# Ubuntu/Debian
sudo apt-get install jq
```

### dotnet-ef bulunamadı hatası
```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:/Users/erdiacar/.dotnet/tools"
```

### Permission denied hatası
```bash
chmod +x scripts/*.sh
```
