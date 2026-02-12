# Client: Takip Edilen Mentor Postlarının (Feed) Dolu Gelmesi

Takip ettiği mentorlerin postlarını almak için client'ın aşağıdaki gibi istek atması gerekir.

## Doğru endpoint

- **URL:** `GET /api/insights/feed`
- **Yanlış:** `GET /api/feed` (bu path API'de yok; 404 alırsınız)

Base URL örnek: `https://your-api.com/api/insights/feed`

---

## 1. Authentication (zorunlu)

Feed endpoint'i **Authorize** ile korunuyor. İstekte mutlaka geçerli bir JWT gönderilmelidir.

**Header:**
```http
Authorization: Bearer <access_token>
```

- Token: Supabase Auth ile giriş sonrası alınan **access token** (JWT).
- Backend, token içindeki `sub` claim'ini kullanıcı ID'si (userId) olarak kullanır.
- Token yok veya geçersizse **401 Unauthorized** döner; bu durumda `insights` hiç dönmez.

**Örnek (fetch):**
```javascript
const response = await fetch('https://your-api.com/api/insights/feed?limit=10&offset=0', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json',
  },
});
```

**Örnek (axios):**
```javascript
const { data } = await axios.get('/api/insights/feed', {
  params: { limit: 10, offset: 0 },
  headers: {
    Authorization: `Bearer ${accessToken}`,
  },
});
// data.insights, data.total, data.hasMore, data.limit, data.offset
```

---

## 2. Query parametreleri

| Parametre | Tip    | Zorunlu | Varsayılan | Açıklama                          |
|-----------|--------|--------|------------|-----------------------------------|
| `tag`     | string | Hayır  | -          | Belirli bir tag’e göre filtreleme |
| `limit`   | number | Hayır  | 5          | Sayfa başına post sayısı          |
| `offset`  | number | Hayır  | 0          | Atlanacak kayıt (pagination)      |

**Örnek URL’ler:**
- İlk 20 post: `GET /api/insights/feed?limit=20&offset=0`
- Sonraki sayfa: `GET /api/insights/feed?limit=20&offset=20`
- Tag ile: `GET /api/insights/feed?tag=growth-marketing&limit=10&offset=0`

---

## 3. Başarılı response (200 OK)

```json
{
  "insights": [
    {
      "id": "guid",
      "mentorId": "guid",
      "content": "...",
      "tags": ["tag1", "tag2"],
      "likeCount": 0,
      "commentCount": 0,
      "createdAt": "2026-02-12T...",
      "mentor": { "id", "name", "role", "level", ... },
      "isLiked": false
    }
  ],
  "total": 50,
  "hasMore": true,
  "limit": 10,
  "offset": 0
}
```

- **insights:** Takip edilen mentorların postları (veya hiç takip yoksa “discover” – en son eklenen postlar).
- **total / hasMore / limit / offset:** Sayfalama bilgisi.

---

## 4. Neden boş (insights = []) dönebilir?

1. **Authorization header yok veya geçersiz**  
   → 401 döner; feed’i hiç çağıramazsınız. Çözüm: Giriş sonrası alınan `access_token` ile `Authorization: Bearer <access_token>` gönderin.

2. **Yanlış path**  
   → `/api/feed` kullanılıyorsa 404. Çözüm: Path’i **`/api/insights/feed`** yapın.

3. **Kullanıcı hiç mentor takip etmiyor**  
   → API “discover” moduna geçer; tüm mentorların en son postlarını döndürür. O anda sistemde post yoksa `insights` boş olur.

4. **Takip edilen mentorların hiç postu yok**  
   → Takip var ama o mentorlara ait Insight kaydı yoksa `insights` boş döner.

**Kontrol listesi (client tarafı):**
- [ ] İstek **GET /api/insights/feed** ile atılıyor mu?
- [ ] Header’da **Authorization: Bearer &lt;access_token&gt;** var mı?
- [ ] Token, giriş yapan kullanıcıya ait ve süresi dolmamış mı?
- [ ] (İsteğe bağlı) `limit` / `offset` ile sayfalama doğru mu?

---

## 5. Özet: Dolu gelmesi için yapılacaklar

1. **Endpoint:** `GET /api/insights/feed`
2. **Header:** `Authorization: Bearer <Supabase access_token>`
3. İsterseniz: `?limit=20&offset=0` (ve sonraki sayfalar için `offset` artırılır).

Bu şekilde istek atıldığında, takip edilen mentorların postları (veya discover) response’taki `insights` alanında dolu gelir; boşsa nedenleri yukarıdaki maddelerle kontrol edilebilir.
