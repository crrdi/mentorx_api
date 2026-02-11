# MentorX API Düzeltmeleri

**Oluşturulma Tarihi:** 2026-02-09  
**Durum:** API tarafında düzeltilmesi gereken sorunlar

Bu doküman `docs/api.md` dosyasında tespit edilen tutarsızlıkları ve eksiklikleri içerir.

---

## 1. GET /api/feed - Response Format Tutarsızlığı

### Sorun
`GET /api/feed` endpoint'inin response formatı `GET /api/insights` ile tutarlı değil. Eksik alanlar var.

### Mevcut Durum (Satır 1535-1560)
```json
{
  "insights": [
    {
      "id": "insight_1",
      "mentorId": "mentor_1",
      "content": "Retention is the silent growth lever...",
      "tags": ["#growth-marketing", "#SaaS"],
      "likeCount": 1200,
      "commentCount": 45,
      "createdAt": "2024-01-25T10:30:00Z",
      "mentor": {...},
      "isLiked": true
    }
  ],
  "total": 50,
  "hasMore": true,
  "limit": 5,
  "offset": 0
}
```

### Eksik Alanlar
Aşağıdaki alanlar `GET /api/insights` response'ında var ama `GET /api/feed` response'ında yok:
- `quote` (string | null)
- `hasMedia` (boolean)
- `mediaUrl` (string | null)
- `updatedAt` (string)
- `editedAt` (string | null)
- `isEdited` (boolean)
- `deletedAt` (string | null)
- `type` (string)
- `masterclassPostId` (string | null)

### Düzeltme
`GET /api/feed` response'ına yukarıdaki alanlar eklenmeli. Response formatı `GET /api/insights` ile aynı olmalı.

### Önerilen Response Formatı
```json
{
  "insights": [
    {
      "id": "insight_1",
      "mentorId": "mentor_1",
      "content": "Retention is the silent growth lever...",
      "quote": "Your best customers are the ones you already have.",
      "tags": ["#growth-marketing", "#SaaS"],
      "likeCount": 1200,
      "commentCount": 45,
      "hasMedia": false,
      "mediaUrl": null,
      "createdAt": "2024-01-25T10:30:00Z",
      "updatedAt": "2024-01-25T10:30:00Z",
      "editedAt": null,
      "isEdited": false,
      "deletedAt": null,
      "type": "insight",
      "masterclassPostId": null,
      "mentor": {
        "id": "mentor_1",
        "name": "Growth Strategy AI",
        "role": "SENIOR MENTOR",
        "level": 8
      },
      "isLiked": true
    }
  ],
  "total": 50,
  "hasMore": true,
  "limit": 5,
  "offset": 0
}
```

---

## 2. GET /api/insights/:id/comments - Response Format Tutarsızlığı

### Sorun
`GET /api/insights/:id/comments` endpoint'i doğrudan array döndürüyor, diğer liste endpoint'leri wrapper object kullanıyor.

### Mevcut Durum (Satır 1618-1658)
```json
[
  {
    "id": "comment_1",
    "insightId": "insight_1",
    "authorActorId": "actor_user_1",
    "content": "Great insight!",
    ...
  }
]
```

### Sorun
- Diğer liste endpoint'leri (`GET /api/insights`, `GET /api/mentors`) wrapper object kullanıyor: `{items: [...], total: ..., hasMore: ...}`
- Bu endpoint doğrudan array döndürüyor
- Pagination bilgisi yok

### Düzeltme
Response formatı wrapper object olmalı ve pagination bilgisi eklenmeli.

### Önerilen Response Formatı
```json
{
  "comments": [
    {
      "id": "comment_1",
      "insightId": "insight_1",
      "authorActorId": "actor_user_1",
      "content": "Great insight! This really resonates...",
      "likeCount": 12,
      "createdAt": "2026-02-09T10:00:00Z",
      "updatedAt": "2026-02-09T10:00:00Z",
      "editedAt": null,
      "isEdited": false,
      "deletedAt": null,
      "parentId": null,
      "author": {
        "id": "user_1",
        "name": "John Doe",
        "type": "user"
      }
    }
  ],
  "total": 10,
  "hasMore": false,
  "limit": 50,
  "offset": 0
}
```

**Not:** Eğer pagination desteklenmeyecekse, en azından wrapper object kullanılmalı:
```json
{
  "comments": [...]
}
```

---

## 3. GET /api/conversations - Pagination Eksikliği

### Sorun
`GET /api/conversations` endpoint'i pagination bilgisi içermiyor ve doğrudan array döndürüyor.

### Mevcut Durum (Satır 1820-1840)
```json
[
  {
    "id": "conv_1",
    "userId": "user_1",
    "mentorId": "mentor_1",
    "lastMessage": "Hello, I have a question...",
    ...
  }
]
```

### Sorun
- Pagination parametreleri yok (`limit`, `offset`)
- Response'da pagination bilgisi yok (`total`, `hasMore`, `limit`, `offset`)
- Doğrudan array döndürüyor (wrapper object yok)

### Düzeltme
1. Query parametreleri eklenmeli: `limit`, `offset`
2. Response formatı wrapper object olmalı
3. Pagination bilgisi eklenmeli

### Önerilen Query Parameters
| Parameter | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `limit` | number | ❌ | 20 | Sayfa başına kayıt sayısı |
| `offset` | number | ❌ | 0 | Atlanacak kayıt sayısı |

### Önerilen Response Formatı
```json
{
  "conversations": [
    {
      "id": "conv_1",
      "userId": "user_1",
      "mentorId": "mentor_1",
      "lastMessage": "Hello, I have a question...",
      "lastMessageAt": "2026-02-09T12:00:00Z",
      "userUnreadCount": 0,
      "createdAt": "2026-02-09T11:00:00Z",
      "updatedAt": "2026-02-09T12:00:00Z",
      "mentor": {
        "id": "mentor_1",
        "name": "Growth Strategy AI",
        "role": "SENIOR MENTOR",
        "level": 8
      }
    }
  ],
  "total": 15,
  "hasMore": false,
  "limit": 20,
  "offset": 0
}
```

---

## 4. Eksik Endpoint: Like/Unlike İşlemleri

### Sorun
`POST /api/insights/:id/like` ve `DELETE /api/insights/:id/like` endpoint'leri dokümantasyonda yok, ancak mock API'de var gibi görünüyor.

### Mevcut Durum
- Dokümantasyonda bu endpoint'ler yok
- Mock API'de (`mock_api/server/index.js`) bu endpoint'ler var olabilir
- `GET /api/insights` response'ında `isLiked` field'ı var, bu endpoint'lerin olması gerektiğini gösteriyor

### Düzeltme
Aşağıdaki endpoint'ler dokümantasyona eklenmeli:

### POST /api/insights/:id/like

**Açıklama:** Bir insight/post'u beğenir.

**Authentication:** ✅ Required

**Path Parameters:**
| Parameter | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string | ✅ | Insight ID |

**Response (200 OK):**
```json
{
  "success": true,
  "liked": true
}
```

**Business Logic:**
1. Token'dan userId çıkarılır
2. Insight bulunur
3. `UserLikes` tablosunda kayıt kontrol edilir:
   - Yoksa: Yeni kayıt eklenir, `Insights.likeCount` artırılır
   - Varsa: Hiçbir şey yapılmaz (idempotent)
4. Transaction içinde işlem yapılır

**Database İşlemleri:**
```sql
-- Insight kontrolü
SELECT * FROM Insights WHERE id = ? AND deletedAt IS NULL;

-- Takip kaydı kontrolü
SELECT * FROM UserLikes WHERE userId = ? AND insightId = ?;

-- Takip kaydı ekleme (yoksa)
INSERT INTO UserLikes (userId, insightId, createdAt)
VALUES (?, ?, NOW())
ON CONFLICT (userId, insightId) DO NOTHING;

-- LikeCount artırma (trigger ile otomatik veya manuel)
UPDATE Insights 
SET likeCount = likeCount + 1
WHERE id = ?;
```

**Error Cases:**
- `401`: Token eksik veya geçersiz
- `404`: Insight bulunamadı

---

### DELETE /api/insights/:id/like

**Açıklama:** Bir insight/post'un beğenisini kaldırır.

**Authentication:** ✅ Required

**Path Parameters:**
| Parameter | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string | ✅ | Insight ID |

**Response (200 OK):**
```json
{
  "success": true,
  "liked": false
}
```

**Business Logic:**
1. Token'dan userId çıkarılır
2. `UserLikes` tablosundan kayıt silinir
3. `Insights.likeCount` azaltılır (min 0)

**Database İşlemleri:**
```sql
-- Takip kaydı silme
DELETE FROM UserLikes 
WHERE userId = ? AND insightId = ?;

-- LikeCount azaltma (trigger ile otomatik veya manuel)
UPDATE Insights 
SET likeCount = GREATEST(0, likeCount - 1)
WHERE id = ?;
```

**Error Cases:**
- `401`: Token eksik veya geçersiz
- `404`: Insight bulunamadı

---

## 5. GET /api/feed - Response'da `total` Alanı Tutarsızlığı

### Sorun
`GET /api/feed` response'ında `total` alanı var (satır 1556), ancak bu tutarlılık açısından kontrol edilmeli.

### Mevcut Durum
- `GET /api/feed` response'ında `total: 50` var (satır 1556)
- `GET /api/insights` response'ında da `total: 100` var (satır 1269)
- Bu tutarlı görünüyor, ancak implementasyonda kontrol edilmeli

### Düzeltme
API implementasyonunda `total` alanının doğru hesaplandığından emin olunmalı.

---

## Özet

### Yapılması Gerekenler

1. ✅ **GET /api/feed** - Eksik alanları ekle (`quote`, `hasMedia`, `mediaUrl`, `updatedAt`, `editedAt`, `isEdited`, `deletedAt`, `type`, `masterclassPostId`)

2. ✅ **GET /api/insights/:id/comments** - Response formatını wrapper object'e çevir ve pagination ekle

3. ✅ **GET /api/conversations** - Pagination parametreleri ve response formatını ekle

4. ✅ **POST /api/insights/:id/like** - Endpoint'i dokümantasyona ekle

5. ✅ **DELETE /api/insights/:id/like** - Endpoint'i dokümantasyona ekle

6. ✅ **GET /api/feed** - `total` alanının doğru hesaplandığını kontrol et

---

## Notlar

- Tüm düzeltmeler yapıldıktan sonra `docs/api.md` dosyası güncellenmelidir
- Mock API (`mock_api/server/index.js`) de bu değişikliklere göre güncellenmelidir
- Flutter uygulamasındaki API client'lar da bu değişikliklere göre güncellenmelidir

---

**Son Güncelleme:** 2026-02-09
