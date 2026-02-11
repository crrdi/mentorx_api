# GitHub Actions Secrets Rehberi

Bu doküman, MentorX API projesi için GitHub Actions'da kullanılması gereken secret'ları ve environment variable'ları açıklar.

## Gerekli Secret'lar

> **⚠️ ÖNEMLİ:** Secret isimleri sadece harf, rakam ve alt çizgi (_) içerebilir. Boşluk kullanılamaz. Harf veya alt çizgi ile başlamalıdır.

### 1. Database Connection String
**Secret Name:** `CONNECTION_STRING`

**Açıklama:** PostgreSQL database connection string (Supabase)

**Format:**
```
Host=aws-1-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.stkeyyhwdfhozteeclvo;Password=YOUR_PASSWORD;Ssl Mode=Require;Trust Server Certificate=true
```

**Kullanım:** 
- `appsettings.json` içinde `ConnectionStrings:DefaultConnection` olarak kullanılır
- GitHub Actions workflow'larında environment variable olarak set edilir

---

### 2. Supabase URL
**Secret Name:** `SUPABASE_URL`

**Açıklama:** Supabase proje URL'i

**Format:**
```
https://stkeyyhwdfhozteeclvo.supabase.co
```

**Kullanım:**
- `appsettings.json` içinde `Supabase:Url` olarak kullanılır

---

### 3. Supabase Anon Key
**Secret Name:** `SUPABASE_ANON_KEY`

**Açıklama:** Supabase anonymous/public key (client-side kullanım için)

**Format:**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Kullanım:**
- `appsettings.json` içinde `Supabase:AnonKey` olarak kullanılır
- Client-side işlemler için kullanılır

---

### 4. Supabase Service Role Key
**Secret Name:** `SUPABASE_SERVICE_ROLE_KEY`

**Açıklama:** Supabase service role key (admin işlemleri için - GİZLİ TUTULMALI)

**Format:**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Kullanım:**
- `appsettings.json` içinde `Supabase:ServiceRoleKey` olarak kullanılır
- Backend admin işlemleri için kullanılır
- ⚠️ **ASLA client-side'da kullanılmamalı**

---

### 5. Gemini API Key
**Secret Name:** `GEMINI_API_KEY`

**Açıklama:** Google Gemini AI API anahtarı

**Format:**
```
AIza...
```

**Kullanım:**
- `appsettings.json` içinde `Gemini:ApiKey` olarak kullanılır
- Environment variable olarak `GEMINI_API_KEY` şeklinde de okunabilir
- AI içerik üretimi için kullanılır

**Alma:** [Google AI Studio](https://makersuite.google.com/app/apikey)

---

### 6. RevenueCat Webhook Secret
**Secret Name:** `REVENUECAT_WEBHOOK_SECRET`

**Açıklama:** RevenueCat webhook doğrulama secret'ı

**Format:**
```
your_webhook_secret_string
```

**Kullanım:**
- `appsettings.json` içinde `RevenueCat:WebhookSecret` olarak kullanılır
- Environment variable olarak `REVENUECAT_WEBHOOK_SECRET` şeklinde de okunabilir
- Webhook endpoint'inde Authorization header doğrulaması için kullanılır

**Not:** RevenueCat dashboard'da webhook URL'si ve Authorization header olarak aynı secret ayarlanmalıdır.

---

## GitHub Actions'a Secret Ekleme

### Adım 1: GitHub Repository'ye Git
1. https://github.com/crrdi/mentorx_api adresine gidin
2. **Settings** sekmesine tıklayın
3. Sol menüden **Secrets and variables** > **Actions** seçin

### Adım 2: Secret Ekleme
1. **New repository secret** butonuna tıklayın
2. Her secret için:
   - **Name:** Secret adını girin (yukarıdaki listeye göre)
   - **Secret:** Secret değerini girin
   - **Add secret** butonuna tıklayın

### Adım 3: Tüm Secret'ları Ekleyin
Aşağıdaki secret'ları ekleyin:

```
CONNECTION_STRING
SUPABASE_URL
SUPABASE_ANON_KEY
SUPABASE_SERVICE_ROLE_KEY
GEMINI_API_KEY
REVENUECAT_WEBHOOK_SECRET
```

---

## GitHub Actions Workflow'larında Kullanım

Secret'lar workflow dosyalarında şu şekilde kullanılır:

```yaml
env:
  CONNECTION_STRING: ${{ secrets.CONNECTION_STRING }}
  SUPABASE_URL: ${{ secrets.SUPABASE_URL }}
  SUPABASE_ANON_KEY: ${{ secrets.SUPABASE_ANON_KEY }}
  SUPABASE_SERVICE_ROLE_KEY: ${{ secrets.SUPABASE_SERVICE_ROLE_KEY }}
  GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
  REVENUECAT_WEBHOOK_SECRET: ${{ secrets.REVENUECAT_WEBHOOK_SECRET }}
```

---

## Local Development için

Local development'ta bu değerleri `appsettings.json` veya User Secrets ile kullanabilirsiniz:

### User Secrets (Önerilen)
```bash
cd MentorX.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "Supabase:Url" "YOUR_SUPABASE_URL"
dotnet user-secrets set "Supabase:AnonKey" "YOUR_ANON_KEY"
dotnet user-secrets set "Supabase:ServiceRoleKey" "YOUR_SERVICE_ROLE_KEY"
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "RevenueCat:WebhookSecret" "YOUR_WEBHOOK_SECRET"
```

### Environment Variables
```bash
export CONNECTION_STRING="YOUR_CONNECTION_STRING"
export SUPABASE_URL="YOUR_SUPABASE_URL"
export SUPABASE_ANON_KEY="YOUR_ANON_KEY"
export SUPABASE_SERVICE_ROLE_KEY="YOUR_SERVICE_ROLE_KEY"
export GEMINI_API_KEY="YOUR_GEMINI_API_KEY"
export REVENUECAT_WEBHOOK_SECRET="YOUR_WEBHOOK_SECRET"
```

---

## Güvenlik Notları

1. ⚠️ **ASLA** secret'ları Git'e commit etmeyin
2. ⚠️ **ASLA** secret'ları log dosyalarına yazmayın
3. ✅ Secret'ları düzenli olarak rotate edin
4. ✅ Production ve Development için farklı secret'lar kullanın
5. ✅ `appsettings.json` dosyasında gerçek değerleri saklamayın (boş bırakın veya placeholder kullanın)

---

## Secret Değerlerini Bulma

### Supabase
1. [Supabase Dashboard](https://supabase.com/dashboard) → Projenizi seçin
2. **Settings** > **API** sayfasına gidin
3. **Project URL** ve **anon public** key'i kopyalayın
4. **Service Role** key'i kopyalayın (gizli tutun!)
5. **Settings** > **Database** > **Connection string** bölümünden connection string'i alın

### Gemini
1. [Google AI Studio](https://makersuite.google.com/app/apikey) adresine gidin
2. Google hesabınızla giriş yapın
3. "Create API Key" butonuna tıklayın
4. Oluşturulan anahtarı kopyalayın

### RevenueCat
1. [RevenueCat Dashboard](https://app.revenuecat.com) → Projenizi seçin
2. **Integrations** > **Webhooks** sayfasına gidin
3. Webhook secret'ınızı oluşturun veya mevcut secret'ı kopyalayın

---

## Troubleshooting

### "Secret not found" Hatası
- GitHub Actions workflow'unda secret adının doğru yazıldığından emin olun
- Repository Settings > Secrets and variables > Actions sayfasında secret'ın eklendiğini kontrol edin

### "Connection string not found" Hatası
- `CONNECTION_STRING` secret'ının eklendiğinden emin olun
- Connection string formatının doğru olduğunu kontrol edin

### "Gemini API key is not configured" Hatası
- `GEMINI_API_KEY` secret'ının eklendiğinden emin olun
- API key'in geçerli olduğunu kontrol edin
