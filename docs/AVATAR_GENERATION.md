# Mentor Avatar Otomatik Oluşturma

Bu doküman, yeni mentor oluşturulduğunda Gemini (Nano Banana) ile otomatik profil resmi üretimi ve Supabase Storage'a yükleme akışını açıklar.

## Genel Bakış

- **Tetikleyici:** `POST /api/mentors` ile yeni mentor oluşturulduğunda
- **Model:** `gemini-2.5-flash-image` (Nano Banana - hızlı image generation)
- **Çıktı:** 1:1 aspect ratio, 1K çözünürlük PNG avatar
- **Depolama:** Supabase Storage `mentor-avatars` bucket'ı
- **Hata davranışı:** Avatar oluşturma başarısız olursa mentor yine oluşturulur, Avatar alanı null kalır

## Mimari Akış

```
CreateMentor Request
    → Mentor DB'ye kayıt
    → SaveChanges
    → GenerateAvatarImageAsync (Gemini)
    → UploadMentorAvatarAsync (Supabase Storage)
    → Mentor.Avatar güncelle
    → Response (avatar URL dahil)
```

## Prompt Yapısı

Mentor bilgilerinden (Name, PublicBio, ExpertiseTags) aşağıdaki prompt üretilir:

```
Create a professional, friendly avatar/profile picture for an AI mentor named "{Name}".
Expertise: {PublicBio}
Topics: {ExpertiseTags}

Style requirements:
- Stylized illustration or abstract portrait (not photorealistic human face)
- Professional, approachable, trustworthy appearance
- Visual elements that subtly reflect the domain (e.g., tech/code aesthetics for developers, leadership symbols for business)
- Clean background, suitable for circular crop
- Square format, centered composition
```

**Not:** Gemini/Imagen person generation politikaları nedeniyle fotogerçekçi insan yüzleri yerine stilize/ilüstratif avatar tercih edilir.

## Güvenlik

- Mentor adı, bio ve tag'ler `SanitizeExpertisePrompt` ile temizlenir (prompt injection önlemi)
- Uzun metinler 2000 karakter ile sınırlanır

## Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `IGeminiService.GenerateAvatarImageAsync` | Avatar image generation interface |
| `GeminiService.GenerateAvatarImageAsync` | Gemini Nano Banana çağrısı |
| `IStorageService.UploadMentorAvatarAsync` | Storage upload interface |
| `SupabaseStorageService` | Supabase Storage upload implementasyonu |
| `MentorService.CreateMentorAsync` | Avatar generation entegrasyonu |
| `scripts/sql/06-mentor-avatars-bucket.sql` | Supabase bucket oluşturma script'i |

## Supabase Setup

1. **Bucket Oluşturma:** `scripts/sql/06-mentor-avatars-bucket.sql` script'ini Supabase SQL Editor'de çalıştırın
2. **Service Role Key:** API, Supabase Service Role Key ile upload yapar - tam yetki gereklidir

## Hata Senaryoları

| Senaryo | Davranış |
|---------|----------|
| Gemini API hatası | Log atılır, Avatar null kalır |
| Storage upload hatası | Log atılır, Avatar null kalır |
| Bucket yok | Storage exception - bucket'ı oluşturun |
| Gemini API key eksik | Mentor oluşturulur, avatar atlanır (try-catch) |

## Performans

- **Avatar generation:** ~2-5 saniye (Gemini 2.5 Flash Image)
- **Storage upload:** ~0.5-1 saniye
- Mentor oluşturma response'u avatar işlemi tamamlandıktan sonra döner (senkron)
