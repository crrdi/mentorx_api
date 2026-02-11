# Comment "User actor not found" Hatası - Çözüm

**Hata:** `HTTP 400: {"error":"User actor not found"}`

**Neden:** Kullanıcının `Actors` tablosunda kaydı yok.

---

## 🔍 Sorunun Nedeni

Backend'de comment oluştururken şu adımlar gerçekleşir:

1. Token'dan `userId` çıkarılır
2. User'ın actor ID'si bulunmaya çalışılır:
   ```sql
   SELECT id FROM Actors WHERE userId = ? AND type = 'user';
   ```
3. Eğer actor kaydı yoksa → **"User actor not found"** hatası

---

## ✅ Çözüm (Backend)

### Seçenek 1: User Oluşturulurken Otomatik Actor Kaydı (Önerilen)

User kaydı oluşturulurken otomatik olarak `Actors` tablosuna kayıt eklenmeli:

```sql
-- User oluşturulduktan sonra
INSERT INTO Actors (id, type, userId, mentorId)
VALUES (
  'actor_user_' || NEW.id,  -- veya UUID
  'user',
  NEW.id,
  NULL
);
```

**Trigger ile otomatik:**
```sql
CREATE OR REPLACE FUNCTION handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.actors (id, type, "userId", "mentorId")
  VALUES (
    'actor_user_' || NEW.id,
    'user',
    NEW.id,
    NULL
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION handle_new_user();
```

### Seçenek 2: Mevcut User'lar İçin Actor Kaydı Oluştur

Mevcut user'lar için actor kayıtları oluştur:

```sql
-- Mevcut user'lar için actor kaydı oluştur
INSERT INTO Actors (id, type, userId, mentorId)
SELECT 
  'actor_user_' || id,
  'user',
  id,
  NULL
FROM Users
WHERE id NOT IN (
  SELECT "userId" FROM Actors WHERE type = 'user' AND "userId" IS NOT NULL
);
```

### Seçenek 3: Comment Endpoint'inde Fallback

Comment endpoint'inde actor kaydı yoksa otomatik oluştur:

```typescript
// Pseudo-code
let actorId = await findActorByUserId(userId);

if (!actorId) {
  // Actor kaydı yoksa oluştur
  actorId = await createActor({
    id: `actor_user_${userId}`,
    type: 'user',
    userId: userId,
    mentorId: null
  });
}
```

---

## 🔧 Backend Agent İçin Prompt

Backend agent'a şu prompt'u gönder:

```
POST /api/insights/:id/comments endpoint'inde "User actor not found" hatası alınıyor.

Sorun: User'ın Actors tablosunda kaydı yok.

Çözüm:
1. User oluşturulurken otomatik olarak Actors tablosuna kayıt eklenmeli
2. Mevcut user'lar için actor kayıtları oluşturulmalı
3. Comment endpoint'inde actor kaydı yoksa otomatik oluşturulmalı (fallback)

Detaylar: docs/comment-actor-fix.md dosyasına bak.
```

---

## 🧪 Test

1. Yeni bir user oluştur
2. Comment atmaya çalış
3. Actor kaydının oluşturulduğunu kontrol et:
   ```sql
   SELECT * FROM Actors WHERE "userId" = 'user_id' AND type = 'user';
   ```

---

## 📝 Kontrol Listesi

- [ ] User oluşturulurken actor kaydı otomatik oluşturuluyor mu?
- [ ] Mevcut user'lar için actor kayıtları var mı?
- [ ] Comment endpoint'inde fallback mekanizması var mı?

---

**Son Güncelleme:** 2026-02-11
