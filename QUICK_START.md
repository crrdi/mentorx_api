# 🚀 Hızlı Başlangıç Rehberi

Bu rehber, MentorX API projesini en hızlı şekilde çalıştırmanızı sağlar.

## ⚡ Hızlı Kurulum (5 Dakika)

### Adım 1: Supabase Access Token Alın

1. https://supabase.com/dashboard/account/tokens adresine gidin
2. "Generate new token" butonuna tıklayın
3. Token'ı kopyalayın

### Adım 2: Otomatik Kurulum Script'ini Çalıştırın

```bash
cd /Users/erdiacar/Desktop/mentorx_api

# Access token'ı ayarlayın
export SUPABASE_ACCESS_TOKEN="your-access-token-here"

# Otomatik kurulum script'ini çalıştırın
./scripts/setup-complete.sh
```

Bu script:
- ✅ Supabase projesi oluşturur
- ✅ appsettings.json'ı günceller
- ✅ Database migration'ları uygular
- ✅ Tüm gerekli adımları tamamlar

### Adım 3: SQL Script'lerini Çalıştırın

1. Supabase Dashboard'a gidin: https://supabase.com/dashboard
2. Oluşturulan projeyi seçin
3. **SQL Editor** sayfasına gidin
4. Şu script'leri sırayla çalıştırın:
   - `scripts/sql/01-rls-policies.sql` - RLS politikaları
   - `scripts/sql/02-triggers.sql` - Database trigger'ları
   - `scripts/sql/03-seed-data.sql` - Başlangıç verileri

### Adım 4: Uygulamayı Çalıştırın

```bash
cd MentorX.API
dotnet run
```

Swagger UI: http://localhost:5000

## 📋 Manuel Kurulum (Script Kullanmak İstemiyorsanız)

Eğer script'leri kullanmak istemiyorsanız:

1. **Supabase Dashboard'dan manuel proje oluşturun**
   - https://supabase.com/dashboard
   - "New Project" butonuna tıklayın
   - Proje bilgilerini doldurun

2. **appsettings.json'ı güncelleyin**
   - `SETUP_SUPABASE.md` dosyasındaki adımları takip edin

3. **Migration'ları uygulayın**
   ```bash
   export PATH="$PATH:/Users/erdiacar/.dotnet/tools"
   dotnet ef database update --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj
   ```

4. **SQL script'lerini çalıştırın**
   - Supabase SQL Editor'de `scripts/sql/` klasöründeki dosyaları sırayla çalıştırın

## 🔧 Gereksinimler

- .NET 8 SDK
- dotnet-ef tools (`dotnet tool install --global dotnet-ef`)
- jq (JSON parser) - macOS: `brew install jq`
- curl ve openssl (genellikle yüklü)

## ✅ Kurulum Kontrolü

Kurulumun başarılı olduğunu kontrol etmek için:

```bash
# 1. Build kontrolü
dotnet build

# 2. Migration kontrolü
dotnet ef migrations list --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj

# 3. Uygulamayı çalıştır
cd MentorX.API
dotnet run
```

## 🐛 Sorun Giderme

### "jq: command not found"
```bash
brew install jq  # macOS
# veya
sudo apt-get install jq  # Linux
```

### "dotnet-ef: command not found"
```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:/Users/erdiacar/.dotnet/tools"
```

### "Permission denied"
```bash
chmod +x scripts/*.sh
```

### Connection String Hatası
- Supabase projesinin aktif olduğundan emin olun
- Database password'ün doğru olduğundan emin olun
- Connection string formatını kontrol edin

## 📚 Daha Fazla Bilgi

- Detaylı kurulum: `SETUP_SUPABASE.md`
- API dokümantasyonu: `docs/api.md`
- Script'ler hakkında: `scripts/README.md`
