# Gemini API Key Configuration Guide

Bu doküman MentorX API projesinde Gemini API anahtarının güvenli bir şekilde nasıl yapılandırılacağını açıklar.

## Güvenlik Notları

⚠️ **ÖNEMLİ:** Gemini API anahtarı asla Git repository'sine commit edilmemelidir. Aşağıdaki yöntemlerden birini kullanarak anahtarı güvenli bir şekilde saklayın.

## Yapılandırma Yöntemleri

### 1. Development Ortamı - User Secrets (Önerilen)

.NET User Secrets kullanarak API anahtarını yerel olarak saklayın:

```bash
# User Secrets'ı başlat (ilk kez)
cd MentorX.API
dotnet user-secrets init

# API anahtarını ekle
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY_HERE"
```

User Secrets otomatik olarak `appsettings.json`'daki değerleri override eder.

### 2. Environment Variables (Production)

Production ortamında environment variable kullanın:

**Linux/macOS:**
```bash
export GEMINI_API_KEY="YOUR_GEMINI_API_KEY_HERE"
```

**Windows (PowerShell):**
```powershell
$env:GEMINI_API_KEY="YOUR_GEMINI_API_KEY_HERE"
```

**Windows (Command Prompt):**
```cmd
set GEMINI_API_KEY=YOUR_GEMINI_API_KEY_HERE
```

**Docker:**
```yaml
environment:
  - GEMINI_API_KEY=YOUR_GEMINI_API_KEY_HERE
```

**Azure App Service:**
1. Azure Portal → App Service → Configuration → Application Settings
2. Yeni bir setting ekle: `GEMINI_API_KEY` = `YOUR_GEMINI_API_KEY_HERE`

### 3. appsettings.json (Sadece Development - Git'e Commit Etmeyin)

⚠️ **UYARI:** Bu yöntem sadece local development için kullanılmalıdır. `appsettings.json` dosyasına gerçek API anahtarı eklemeyin.

`appsettings.json` dosyasında şu şekilde boş bırakın:

```json
{
  "Gemini": {
    "ApiKey": ""
  }
}
```

Gerçek anahtar User Secrets veya Environment Variables ile sağlanmalıdır.

## Yapılandırma Önceliği

GeminiService şu sırayla API anahtarını arar:

1. `Gemini:ApiKey` (appsettings.json veya User Secrets)
2. `GEMINI_API_KEY` (Environment Variable)

İlk bulduğu değeri kullanır.

## Gemini API Anahtarı Alma

1. [Google AI Studio](https://makersuite.google.com/app/apikey) adresine gidin
2. Google hesabınızla giriş yapın
3. "Create API Key" butonuna tıklayın
4. Oluşturulan anahtarı kopyalayın ve yukarıdaki yöntemlerden biriyle yapılandırın

## Test Etme

API anahtarının doğru yapılandırıldığını test etmek için:

```bash
# API'yi çalıştır
cd MentorX.API
dotnet run

# Bir post oluşturmayı deneyin (Swagger UI'dan veya Postman ile)
POST /api/insights
```

Eğer API anahtarı eksikse veya geçersizse, hata mesajı alırsınız.

## Troubleshooting

### "Gemini API key is not configured" Hatası

- User Secrets kullanıyorsanız: `dotnet user-secrets list` ile kontrol edin
- Environment variable kullanıyorsanız: `echo $GEMINI_API_KEY` (Linux/macOS) veya `echo %GEMINI_API_KEY%` (Windows) ile kontrol edin
- `appsettings.json` dosyasında `Gemini:ApiKey` alanının var olduğundan emin olun

### "Gemini API key cannot be empty" Hatası

- API anahtarının boş olmadığından emin olun
- Tırnak işaretleri veya ekstra boşluklar olmadığından emin olun

## .gitignore Kontrolü

Aşağıdaki dosyaların `.gitignore` dosyasında olduğundan emin olun:

```
# User Secrets
**/Properties/launchSettings.json
**/Properties/userSecrets.json

# appsettings with secrets (eğer varsa)
**/appsettings.Development.json
**/appsettings.Production.json
```

## Güvenlik Best Practices

1. ✅ **YAPIN:** User Secrets veya Environment Variables kullanın
2. ✅ **YAPIN:** Production'da environment variables kullanın
3. ✅ **YAPIN:** API anahtarını düzenli olarak rotate edin
4. ❌ **YAPMAYIN:** API anahtarını Git'e commit etmeyin
5. ❌ **YAPMAYIN:** API anahtarını log dosyalarına yazmayın
6. ❌ **YAPMAYIN:** API anahtarını client-side kodda kullanmayın
