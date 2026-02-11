# Gemini API Key Kontrolü

## Sorun
Post oluştururken "Generated content based on..." gibi placeholder metin görünüyor. Bu, Gemini API key'inin yapılandırılmadığı veya geçersiz olduğu anlamına gelir.

## Çözüm

### 1. API Key'i Kontrol Et

Terminal'de şu komutu çalıştır:

```bash
cd MentorX.API
dotnet user-secrets list
```

Eğer `Gemini:ApiKey` görünmüyorsa veya boşsa, şu komutu çalıştır:

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY_HERE"
```

### 2. Environment Variable Kontrolü

Alternatif olarak environment variable kullanabilirsin:

```bash
# macOS/Linux
export GEMINI_API_KEY="YOUR_GEMINI_API_KEY_HERE"

# Windows PowerShell
$env:GEMINI_API_KEY="YOUR_GEMINI_API_KEY_HERE"
```

### 3. API Key Alma

1. [Google AI Studio](https://makersuite.google.com/app/apikey) adresine git
2. Google hesabınla giriş yap
3. "Create API Key" butonuna tıkla
4. Oluşturulan anahtarı kopyala

### 4. Test Et

API'yi yeniden başlat ve bir post oluşturmayı dene:

```bash
cd MentorX.API
dotnet run
```

Eğer hala placeholder metin görüyorsan, log dosyalarını kontrol et:

```bash
tail -f logs/mentorx-*.txt
```

Log'larda şu hatalardan birini görebilirsin:
- "Gemini API key is not configured"
- "Gemini API key cannot be empty"
- "Failed to generate content using Gemini"

## Notlar

- `appsettings.json` dosyasında `Gemini:ApiKey` boş bırakılmalı (güvenlik için)
- Gerçek API key User Secrets veya Environment Variable'da saklanmalı
- API key asla Git'e commit edilmemeli
