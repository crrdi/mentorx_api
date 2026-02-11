# Actor Kaydı Nedir ve Neden Gerekli?

## Actor Pattern Nedir?

**Actor**, sistemdeki "aktörleri" (kimlikleri) temsil eden bir tablodur. Hem **User** hem de **Mentor** kimliklerini tek bir tablo üzerinden yönetir.

## Neden Actor Tablosu Var?

### 1. Polymorphic İlişki (Çok Biçimli İlişki)

Sistemde hem **User** hem de **Mentor** tarafından içerik üretilebilir:

- **Comment**: Hem user hem mentor comment atabilir
- **Message**: Hem user hem mentor mesaj gönderebilir
- **Insight**: Mentor tarafından oluşturulur

Bu durumda iki seçenek var:

**Seçenek 1: Ayrı Tablolar (Kötü)**
```sql
-- Comment tablosu
authorUserId UUID NULL
authorMentorId UUID NULL
-- Hangisi dolu? Kontrol etmek gerekir, karmaşık
```

**Seçenek 2: Actor Pattern (İyi) ✅**
```sql
-- Comment tablosu
authorActorId UUID NOT NULL
-- Actor tablosuna bak, type'a göre user veya mentor olduğunu anla
```

### 2. Actor Tablosu Yapısı

```sql
CREATE TABLE Actors (
  id UUID PRIMARY KEY,
  type INTEGER NOT NULL,  -- 1: User, 2: Mentor
  userId UUID NULL,       -- Eğer type=User ise dolu
  mentorId UUID NULL,     -- Eğer type=Mentor ise dolu
  ...
)
```

**Kurallar:**
- `type = User` → `userId` dolu, `mentorId` NULL
- `type = Mentor` → `mentorId` dolu, `userId` NULL
- Her user'ın **mutlaka** bir actor kaydı olmalı
- Her mentor'ın **mutlaka** bir actor kaydı olmalı

### 3. Kullanım Senaryoları

#### Senaryo 1: User Comment Atıyor
```
1. User login olur → userId = "abc-123"
2. Comment oluşturur → authorActorId = Actor(userId="abc-123").id
3. Actor tablosundan userId'yi bul → User bilgilerini göster
```

#### Senaryo 2: Mentor Comment Atıyor (AI Generated)
```
1. User bir mentor'ı seçer → mentorId = "xyz-789"
2. Gemini ile comment generate edilir
3. Comment oluşturulur → authorActorId = Actor(mentorId="xyz-789").id
4. Actor tablosundan mentorId'yi bul → Mentor bilgilerini göster
```

#### Senaryo 3: DM Mesajı
```
1. User mesaj gönderir → senderActorId = Actor(userId="abc-123").id
2. Mentor otomatik cevap verir → senderActorId = Actor(mentorId="xyz-789").id
3. Her ikisi de aynı tabloda (Messages), sadece actorId farklı
```

## Actor Kaydı Oluşturma Zorunluluğu

### User İçin Actor Kaydı

**Zorunlu:** Her user'ın mutlaka bir actor kaydı olmalı.

**Oluşturulma Yerleri:**
1. ✅ **Database Trigger** (`scripts/sql/02-triggers.sql`)
   - `auth.users` tablosuna INSERT olduğunda otomatik oluşturulur
   - En güvenilir yöntem

2. ✅ **AuthService** (`GoogleAuthAsync`, `AppleAuthAsync`)
   - Yeni user oluşturulurken actor kaydı oluşturulur
   - Fallback mekanizması (trigger çalışmazsa)

### Mentor İçin Actor Kaydı

**Zorunlu:** Her mentor'ın mutlaka bir actor kaydı olmalı.

**Oluşturulma Yeri:**
- ✅ **MentorService** (`CreateMentorAsync`)
  - Mentor oluşturulurken otomatik actor kaydı oluşturulur

## Veri Bütünlüğü Kontrolü

### ❌ YANLIŞ: Fallback Mekanizması
```csharp
// CommentService'de
var actor = await GetActor(userId);
if (actor == null) {
    actor = CreateActor(userId); // ❌ Fallback - kötü pratik
}
```

**Neden Kötü?**
- Veri bütünlüğü bozulur
- User kaydı eksikse bile çalışır (sessiz hata)
- Sorunun kaynağını gizler

### ✅ DOĞRU: Strict Validation
```csharp
// CommentService'de
var actor = await GetActor(userId);
if (actor == null) {
    throw new InvalidOperationException(
        "User actor not found. User must be properly registered."
    );
}
```

**Neden İyi?**
- Veri bütünlüğü korunur
- Sorun hemen görülür ve düzeltilir
- User kayıt sürecindeki hatayı gösterir

## Sorun Giderme

### "User actor not found" Hatası Alınıyorsa

**Kontrol Listesi:**
1. ✅ Database trigger çalışıyor mu?
   ```sql
   SELECT * FROM pg_trigger WHERE tgname = 'on_auth_user_created';
   ```

2. ✅ Trigger fonksiyonu doğru mu?
   ```sql
   SELECT * FROM pg_proc WHERE proname = 'handle_new_user';
   ```

3. ✅ Mevcut user'lar için actor kaydı var mı?
   ```sql
   SELECT u.id, u.email, a.id as actor_id
   FROM users u
   LEFT JOIN actors a ON a."userId" = u.id AND a.type = 1
   WHERE a.id IS NULL;
   ```

4. ✅ AuthService actor oluşturuyor mu?
   - `GoogleAuthAsync` ve `AppleAuthAsync` metodlarını kontrol et

### Çözüm: Mevcut User'lar İçin Actor Kaydı Oluştur

```sql
-- Mevcut user'lar için actor kaydı oluştur (tek seferlik)
INSERT INTO actors (id, type, "userId", "mentorId", "createdAt", "updatedAt")
SELECT 
  gen_random_uuid(),
  1,  -- User type
  u.id,
  NULL,
  NOW(),
  NOW()
FROM users u
WHERE NOT EXISTS (
  SELECT 1 FROM actors a 
  WHERE a."userId" = u.id AND a.type = 1
);
```

## Özet

**Actor Kaydı:**
- ✅ User ve Mentor kimliklerini tek tabloda yönetir
- ✅ Comment ve Message'ların kim tarafından oluşturulduğunu gösterir
- ✅ Polymorphic ilişki sağlar
- ✅ Her user ve mentor için **zorunludur**

**Veri Girişi:**
- ✅ Database trigger ile otomatik oluşturulmalı
- ✅ AuthService fallback olarak oluşturmalı
- ✅ Fallback mekanizması **kullanılmamalı** (strict validation)
- ✅ Actor kaydı yoksa **hata dönülmeli**
