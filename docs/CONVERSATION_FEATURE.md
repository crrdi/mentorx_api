# Conversation Feature Documentation - MentorX

Bu dokümantasyon, MentorX uygulamasındaki konuşma (conversation/DM) özelliğinin client tarafından nasıl kullanılacağını açıklar.

## 1. Feature Overview

Conversation özelliği, kullanıcıların AI mentor'ları ile doğrudan mesajlaşma (DM) yapmasını sağlar. Her mesaj gönderildiğinde:

- Kullanıcının kredisi kontrol edilir (en az 1 kredi olmalı)
- Mesaj kaydedilir
- 1 kredi düşürülür ve transaction kaydı oluşturulur
- Mentor otomatik olarak Gemini AI kullanarak cevap üretir

### Önemli Notlar

- **Kredi Sistemi**: Her mesaj gönderme işlemi 1 kredi kullanır
- **Mentor Cevabı**: Mentor cevapları senkron olarak üretilir ve response'da döner - client'in polling yapmasına gerek yok
- **Kredi Düşürme**: Kredi, mesaj başarıyla kaydedildikten sonra düşürülür
- **Hata Durumu**: Gemini API hatası olsa bile kullanıcının kredisi düşürülmüş olur ve `mentorReply` `null` döner (kullanıcı mesaj göndermek için ödeme yapıyor, cevap almak için değil)

## 2. API Endpoints

### 2.1 Konuşma Listesi Getirme

**Endpoint:** `GET /api/conversations`

**Query Parameters:**
- `limit` (optional, default: 20): Sayfa başına kayıt sayısı
- `offset` (optional, default: 0): Başlangıç pozisyonu

**Response:**
```json
{
  "conversations": [
    {
      "id": "conv_123",
      "userId": "user_1",
      "mentorId": "mentor_1",
      "lastMessage": "Hello, how can I help?",
      "lastMessageAt": "2026-02-12T10:00:00Z",
      "userUnreadCount": 0,
      "createdAt": "2026-02-10T10:00:00Z",
      "updatedAt": "2026-02-12T10:00:00Z"
    }
  ],
  "total": 5,
  "hasMore": false,
  "limit": 20,
  "offset": 0
}
```

### 2.2 Yeni Konuşma Oluşturma

**Endpoint:** `POST /api/conversations`

**Request:**
```json
{
  "mentorId": "mentor_1"
}
```

**Response (201 Created):**
```json
{
  "id": "conv_123",
  "userId": "user_1",
  "mentorId": "mentor_1",
  "lastMessage": "",
  "lastMessageAt": "2026-02-12T10:00:00Z",
  "userUnreadCount": 0,
  "createdAt": "2026-02-12T10:00:00Z",
  "updatedAt": "2026-02-12T10:00:00Z"
}
```

**Not:** Eğer kullanıcı ile mentor arasında zaten bir konuşma varsa, mevcut konuşma döner (yeni konuşma oluşturulmaz).

### 2.3 Mesajları Getirme

**Endpoint:** `GET /api/conversations/:id/messages`

**Path Parameters:**
- `id`: Conversation ID

**Query Parameters:**
- `limit` (optional, default: 50): Sayfa başına mesaj sayısı
- `offset` (optional, default: 0): Başlangıç pozisyonu

**Response:**
```json
{
  "messages": [
    {
      "id": "msg_1",
      "conversationId": "conv_123",
      "senderActorId": "actor_user_1",
      "content": "Hello, I have a question about growth strategies.",
      "createdAt": "2026-02-12T10:00:00Z",
      "updatedAt": "2026-02-12T10:00:00Z",
      "editedAt": null,
      "isEdited": false,
      "deletedAt": null,
      "sender": {
        "id": "user_1",
        "name": "John Doe",
        "type": "user"
      }
    },
    {
      "id": "msg_2",
      "conversationId": "conv_123",
      "senderActorId": "actor_mentor_1",
      "content": "I'd be happy to help! What specific aspect of growth strategies are you interested in?",
      "createdAt": "2026-02-12T10:00:05Z",
      "updatedAt": "2026-02-12T10:00:05Z",
      "editedAt": null,
      "isEdited": false,
      "deletedAt": null,
      "sender": {
        "id": "mentor_1",
        "name": "Growth Mentor AI",
        "type": "mentor"
      }
    }
  ],
  "total": 2,
  "hasMore": false,
  "limit": 50,
  "offset": 0
}
```

**Notlar:**
- Mesajlar `CreatedAt` sırasına göre artan sırada döner (en eski mesaj ilk)
- Pagination ile eski mesajları yükleyebilirsiniz
- `hasMore: true` ise daha fazla mesaj var demektir

### 2.4 Mesaj Gönderme

**Endpoint:** `POST /api/conversations/:id/messages`

**Path Parameters:**
- `id`: Conversation ID

**Request:**
```json
{
  "content": "Hello, I have a question about growth strategies."
}
```

**Response (201 Created):**
```json
{
  "userMessage": {
    "id": "msg_123",
    "conversationId": "conv_1",
    "senderActorId": "actor_user_1",
    "content": "Hello, I have a question about growth strategies.",
    "createdAt": "2026-02-12T10:00:00Z",
    "updatedAt": "2026-02-12T10:00:00Z",
    "editedAt": null,
    "isEdited": false,
    "deletedAt": null,
    "sender": {
      "id": "user_1",
      "name": "John Doe",
      "type": "user"
    }
  },
  "mentorReply": {
    "id": "msg_124",
    "conversationId": "conv_1",
    "senderActorId": "actor_mentor_1",
    "content": "I'd be happy to help! What specific aspect of growth strategies are you interested in?",
    "createdAt": "2026-02-12T10:00:05Z",
    "updatedAt": "2026-02-12T10:00:05Z",
    "editedAt": null,
    "isEdited": false,
    "deletedAt": null,
    "sender": {
      "id": "mentor_1",
      "name": "Growth Mentor AI",
      "type": "mentor"
    }
  }
}
```

**Önemli Notlar:**
- Response'da hem kullanıcı mesajı (`userMessage`) hem de mentor cevabı (`mentorReply`) döner
- Mentor cevabı senkron olarak generate edilir - client'in polling yapmasına gerek yok
- `mentorReply` alanı `null` olabilir eğer Gemini API hatası oluşursa (bu durumda sadece `userMessage` döner)

## 3. Kredi Sistemi

### 3.1 Kredi Kontrolü

Mesaj göndermeden önce kullanıcının kredisi kontrol edilir:

- **Minimum Gereksinim**: En az 1 kredi olmalı
- **Yetersiz Kredi**: `400 Bad Request` hatası döner
  ```json
  {
    "error": "Insufficient credits"
  }
  ```

### 3.2 Kredi Düşürme

Mesaj başarıyla kaydedildikten sonra:

1. Kullanıcının kredisi 1 azaltılır
2. `CreditTransaction` kaydı oluşturulur:
   - `Type`: `Deduction`
   - `Amount`: `-1`
   - `BalanceAfter`: Yeni kredi bakiyesi

### 3.3 Kredi Bakiyesi Kontrolü

Kullanıcının kredi bakiyesini kontrol etmek için:

**Endpoint:** `GET /api/credits/balance`

**Response:**
```json
{
  "balance": 5
}
```

## 4. Mesaj Güncelleme ve Yeni Mesajlar

### 4.1 Yeni Mesaj Geldiğinde Update

Her yeni mesaj (kullanıcı veya mentor) gönderildiğinde:

1. **Mesaj DB'ye kaydedilir** → `Messages` tablosuna INSERT
2. **Conversation güncellenir:**
   - `lastMessage`: Son mesajın içeriği
   - `lastMessageAt`: Son mesajın zamanı
   - `updatedAt`: Güncelleme zamanı
3. **Kullanıcı mesajı ise:** Kredi düşürülür ve transaction kaydı oluşturulur
4. **Mentor cevabı ise:** Background task ile Gemini'den generate edilir ve kaydedilir

### 4.2 Eski Mesajları Görüntüleme

Eski mesajları görmek için pagination kullanın:

**Örnek: İlk 50 mesajı getir**
```typescript
GET /api/conversations/{id}/messages?limit=50&offset=0
```

**Örnek: Sonraki 50 mesajı getir**
```typescript
GET /api/conversations/{id}/messages?limit=50&offset=50
```

**Örnek: Son 20 mesajı getir (en yeni mesajlar)**
```typescript
// Total mesaj sayısını bilmeniz gerekir
const total = response.total;
const last20Offset = Math.max(0, total - 20);
GET /api/conversations/{id}/messages?limit=20&offset=${last20Offset}
```

**Client-side örnek:**
```typescript
async function loadMessages(conversationId: string, limit = 50, offset = 0) {
  const response = await fetch(
    `/api/conversations/${conversationId}/messages?limit=${limit}&offset=${offset}`,
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  const data = await response.json();
  
  // Mesajları ekranınıza ekleyin
  data.messages.forEach(message => {
    displayMessage(message);
  });
  
  // Daha fazla mesaj varsa "Load More" butonu göster
  if (data.hasMore) {
    showLoadMoreButton(() => {
      loadMessages(conversationId, limit, offset + limit);
    });
  }
}
```

### 4.3 Real-time Update (Artık Gerekli Değil)

**ÖNEMLİ:** Mentor cevabı artık senkron olarak response'da döndüğü için polling'e gerek yok!

Mesaj gönderildiğinde response'da hem kullanıcı mesajı hem de mentor cevabı gelir:

```typescript
const response = await fetch(`/api/conversations/${conversationId}/messages`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({ content: "Hello!" })
});

const data = await response.json();

// Kullanıcı mesajını göster
displayMessage(data.userMessage);

// Mentor cevabını göster (eğer varsa)
if (data.mentorReply) {
  displayMessage(data.mentorReply);
} else {
  // Hata durumu - mentor cevabı generate edilemedi
  showError("Mentor cevabı şu anda alınamadı. Lütfen tekrar deneyin.");
}
```

## 5. Mentor Cevap Üretme Mekanizması

### 5.1 Otomatik Cevap (Senkron)

Kullanıcı mesaj gönderdikten sonra:

1. Sistem senkron olarak mentor cevabı üretir
2. Mentor'un `expertisePrompt` ve `name` bilgileri kullanılır (tag'ler kullanılmaz - konuşma odaklı)
3. Gemini AI ile son 10 mesajın geçmişi göz önünde bulundurularak cevap üretilir
4. Üretilen cevap DB'ye kaydedilir ve response'da `mentorReply` alanında döner
5. Client'in polling yapmasına gerek yok - cevap direkt response'da gelir

**Cevap Özellikleri:**
- **Uzunluk:** 400-1000 karakter arası
- **Stil:** Doğal, konuşur gibi yazılmış (sosyal medya postu gibi değil)
- **Format:** Hashtag, tag veya post benzeri format kullanılmaz
- **Ton:** Sıcak, samimi, yardımcı ve doğrudan konuşma tarzı

### 5.2 Cevap Üretme Süresi

- Mentor cevapları genellikle birkaç saniye içinde üretilir
- Gemini API yanıt süresine bağlıdır (genellikle 2-5 saniye)
- Daha uzun cevaplar (400-1000 karakter) biraz daha uzun sürebilir
- HTTP request bu süre boyunca bekler ve cevap geldiğinde response döner
- Client tarafında loading indicator gösterilebilir

### 5.3 Hata Durumları

Eğer mentor cevabı üretilemezse:

- Kullanıcının kredisi zaten düşürülmüş olur (bu doğru davranış)
- Hata log'a kaydedilir
- Response'da `mentorReply` alanı `null` döner
- `userMessage` başarıyla kaydedilmiş olur
- Kullanıcı tekrar mesaj gönderebilir

## 6. Error Handling

### 6.1 Yaygın Hatalar

**401 Unauthorized**
```json
{
  "error": "Unauthorized"
}
```
- Token eksik veya geçersiz
- Çözüm: Kullanıcıyı login sayfasına yönlendir

**400 Bad Request - Insufficient Credits**
```json
{
  "error": "Insufficient credits"
}
```
- Kullanıcının yeterli kredisi yok
- Çözüm: Kredi satın alma sayfasına yönlendir

**403 Forbidden**
```json
{
  "error": "Conversation not found or access denied"
}
```
- Konuşma bulunamadı veya kullanıcıya ait değil
- Çözüm: Konuşma listesine yönlendir

**404 Not Found**
```json
{
  "error": "Mentor not found"
}
```
- Mentor bulunamadı
- Çözüm: Mentor listesine yönlendir

### 6.2 Client-Side Error Handling Örneği

```typescript
async function sendMessage(conversationId: string, content: string) {
  try {
    const response = await fetch(`/api/conversations/${conversationId}/messages`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ content })
    });

    if (!response.ok) {
      const error = await response.json();
      
      if (error.error === 'Insufficient credits') {
        // Kredi yetersiz - kredi satın alma sayfasına yönlendir
        router.push('/credits/purchase');
        return;
      }
      
      throw new Error(error.error || 'Failed to send message');
    }

    const message = await response.json();
    return message;
  } catch (error) {
    console.error('Error sending message:', error);
    // Kullanıcıya hata mesajı göster
    showErrorToast('Failed to send message. Please try again.');
  }
}
```

## 7. Best Practices

### 7.1 Mesaj Gönderme

- Mesaj göndermeden önce kredi bakiyesini kontrol et
- Kullanıcıya kredi durumunu göster
- Mesaj gönderildiğinde loading indicator göster (Gemini API yanıt süresine bağlı, genellikle 2-5 saniye)
- Response'da hem kullanıcı mesajı hem de mentor cevabı gelir - polling'e gerek yok

### 7.2 Kullanıcı Deneyimi

- Mesaj göndermeden önce kredi uyarısı göster
- Yetersiz kredi durumunda kredi satın alma sayfasına yönlendir
- Mesaj gönderildiğinde loading indicator göster (cevap gelene kadar)
- Response'da gelen mesajları direkt ekrana ekle
- Eğer `mentorReply` `null` ise hata mesajı göster

### 8. Örnek Kullanım Senaryosu

```typescript
// 1. Kredi bakiyesini kontrol et
const balance = await getCreditBalance();
if (balance < 1) {
  showInsufficientCreditsModal();
  return;
}

// 2. Konuşma oluştur veya mevcut konuşmayı al
let conversation = await getOrCreateConversation(mentorId);

// 3. Mesaj gönder (loading göster)
showLoadingIndicator();
try {
  const response = await sendMessage(conversation.id, "Hello!");
  
  // 4. Kullanıcı mesajını ekle
  displayMessage(response.userMessage);
  
  // 5. Mentor cevabını ekle (eğer varsa)
  if (response.mentorReply) {
    displayMessage(response.mentorReply);
  } else {
    showError("Mentor cevabı şu anda alınamadı.");
  }
} catch (error) {
  showError("Mesaj gönderilemedi: " + error.message);
} finally {
  hideLoadingIndicator();
}
```

## 9. Teknik Detaylar

### 9.1 Mentor Cevap Üretme

Mentor cevapları şu bilgiler kullanılarak üretilir:

- **Mentor Name**: Mentor'un adı
- **Mentor Handle**: `mentor_{id.substring(0, 8)}`
- **Expertise Prompt**: Mentor'un özel sistem prompt'u (conversation-specific prompt kullanılır)
- **Conversation History**: Son 10 mesaj
- **User Message**: Kullanıcının gönderdiği mesaj

**Prompt Özellikleri:**
- **Conversation-Specific System Prompt**: Tag'ler kullanılmaz, konuşma odaklı prompt kullanılır
- **Doğal Konuşma Stili**: Sosyal medya postu gibi değil, doğrudan konuşma tarzı
- **Uzunluk Hedefi**: 400-1000 karakter arası
- **Format Kısıtlamaları**: Hashtag, tag veya post benzeri format kullanılmaz

### 9.2 Güvenlik

- Tüm user input'ları sanitize edilir (prompt injection önleme)
- Conversation ownership kontrolü yapılır
- Kredi işlemleri transaction içinde gerçekleştirilir

### 9.3 Performans

- Mentor cevapları asenkron olarak üretilir (non-blocking)
- Mesaj listesi sayfalama ile optimize edilir
- Conversation listesi cache'lenebilir

## 10. API Rate Limits

Şu anda rate limit yoktur, ancak gelecekte eklenebilir. Client tarafında:

- Mesaj gönderme işlemlerini throttle et
- Çok sık polling yapma
- WebSocket kullanımını tercih et (gelecekte)

## 11. Veritabanı Yapısı ve Mesaj Saklama

### 11.1 Veritabanı Şeması

Konuşma ve mesajlar şu tablolarda saklanır:

#### Conversations Tablosu
```sql
CREATE TABLE "Conversations" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL,              -- Konuşmayı başlatan kullanıcı
    "MentorId" uuid NOT NULL,            -- Konuşulan mentor
    "LastMessage" text NOT NULL,          -- Son mesajın içeriği (preview için)
    "LastMessageAt" timestamp NOT NULL,   -- Son mesajın zamanı
    "UserUnreadCount" integer DEFAULT 0, -- Kullanıcının okunmamış mesaj sayısı
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL,
    "DeletedAt" timestamp                -- Soft delete için
);
```

**İlişkiler:**
- `UserId` → `Users.Id` (Foreign Key)
- `MentorId` → `Mentors.Id` (Foreign Key)

#### Messages Tablosu
```sql
CREATE TABLE "Messages" (
    "Id" uuid PRIMARY KEY,
    "ConversationId" uuid NOT NULL,      -- Hangi konuşmaya ait
    "SenderActorId" uuid NOT NULL,      -- Mesajı gönderen (User veya Mentor)
    "Content" text NOT NULL,             -- Mesaj içeriği
    "EditedAt" timestamp,                -- Düzenlenme zamanı (null ise düzenlenmemiş)
    "IsEdited" boolean DEFAULT false,    -- Düzenlenmiş mi?
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL,
    "DeletedAt" timestamp                 -- Soft delete için
);
```

**İlişkiler:**
- `ConversationId` → `Conversations.Id` (Foreign Key)
- `SenderActorId` → `Actors.Id` (Foreign Key)

#### Actors Tablosu
```sql
CREATE TABLE "Actors" (
    "Id" uuid PRIMARY KEY,
    "Type" integer NOT NULL,             -- ActorType enum: User=1, Mentor=2
    "UserId" uuid,                       -- Eğer Type=User ise bu alan dolu
    "MentorId" uuid,                     -- Eğer Type=Mentor ise bu alan dolu
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL,
    "DeletedAt" timestamp
);
```

**Açıklama:** `Actors` tablosu, hem kullanıcıların hem de mentor'ların mesaj gönderebilmesi için ortak bir yapı sağlar. Bu sayede `Messages` tablosunda tek bir `SenderActorId` ile hem kullanıcı hem mentor mesajları saklanabilir.

### 11.2 Mesaj Saklama Akışı

#### Kullanıcı Mesajı Gönderdiğinde:

1. **Message Kaydı Oluşturulur:**
   ```csharp
   var message = new Message {
       Id = Guid.NewGuid(),
       ConversationId = conversationId,
       SenderActorId = userActor.Id,  // User'ın actor ID'si
       Content = "Kullanıcı mesajı",
       CreatedAt = DateTime.UtcNow,
       UpdatedAt = DateTime.UtcNow
   };
   ```

2. **Conversation Güncellenir:**
   ```csharp
   conversation.LastMessage = message.Content;
   conversation.LastMessageAt = DateTime.UtcNow;
   conversation.UpdatedAt = DateTime.UtcNow;
   ```

3. **Veritabanına Kaydedilir:**
   - `Messages` tablosuna INSERT
   - `Conversations` tablosunda UPDATE

#### Mentor Cevabı Generate Edildiğinde:

1. **Gemini API'den Cevap Alınır:**
   - Mentor'un `ExpertisePrompt`, `Tags`, `Name` bilgileri kullanılır
   - Son 10 mesajın geçmişi göz önünde bulundurulur
   - Gemini AI ile cevap üretilir

2. **Mentor Mesajı Oluşturulur:**
   ```csharp
   var mentorReply = new Message {
       Id = Guid.NewGuid(),
       ConversationId = conversationId,
       SenderActorId = mentorActor.Id,  // Mentor'ın actor ID'si
       Content = "Mentor cevabı (Gemini'den gelen)",
       CreatedAt = DateTime.UtcNow,
       UpdatedAt = DateTime.UtcNow
   };
   ```

3. **Veritabanına Kaydedilir:**
   - `Messages` tablosuna INSERT
   - `Conversations` tablosunda UPDATE (lastMessage ve lastMessageAt)

### 11.3 Mesaj Sorgulama

#### Tüm Mesajları Getirme:
```sql
SELECT m.*, a.Type as SenderType
FROM Messages m
JOIN Actors a ON m.SenderActorId = a.Id
WHERE m.ConversationId = @conversationId
  AND m.DeletedAt IS NULL
ORDER BY m.CreatedAt ASC;
```

#### Mesaj Gönderen Bilgisini Bulma:
```sql
-- Eğer Actor Type = User ise
SELECT u.Id, u.Name, 'user' as Type
FROM Messages m
JOIN Actors a ON m.SenderActorId = a.Id
JOIN Users u ON a.UserId = u.Id
WHERE m.Id = @messageId AND a.Type = 1;

-- Eğer Actor Type = Mentor ise
SELECT m.Id, m.Name, 'mentor' as Type
FROM Messages msg
JOIN Actors a ON msg.SenderActorId = a.Id
JOIN Mentors m ON a.MentorId = m.Id
WHERE msg.Id = @messageId AND a.Type = 2;
```

### 11.4 Veri Yapısı Özeti

```
Conversation (1) ──< (N) Messages
    │                      │
    │                      └──> Actor (SenderActorId)
    │                              │
    │                              ├──> User (eğer Type=User)
    │                              └──> Mentor (eğer Type=Mentor)
    │
    ├──> User (UserId)
    └──> Mentor (MentorId)
```

### 11.5 Önemli Notlar

1. **Actor Pattern:** Mesajlar doğrudan User veya Mentor'a bağlı değil, `Actor` üzerinden bağlıdır. Bu sayede:
   - Hem kullanıcı hem mentor mesaj gönderebilir
   - Tek bir `SenderActorId` ile her iki durum da yönetilir
   - Gelecekte başka actor tipleri eklenebilir

2. **Soft Delete:** Tüm tablolarda `DeletedAt` alanı vardır. Silinen kayıtlar fiziksel olarak silinmez, sadece `DeletedAt` alanı doldurulur.

3. **Conversation Güncelleme:** Her mesaj gönderildiğinde `Conversations` tablosundaki `LastMessage` ve `LastMessageAt` alanları güncellenir. Bu sayede konuşma listesinde son mesaj preview olarak gösterilebilir.

4. **Mesaj Geçmişi:** Mentor cevabı üretilirken son 10 mesaj alınır ve Gemini'ye context olarak gönderilir. Bu sayede mentor konuşmanın bağlamını anlayabilir.

5. **Timestamp'ler:** Tüm timestamp'ler UTC olarak saklanır (`DateTime.UtcNow` kullanılır).

## 12. Destek

Sorularınız için:
- API dokümantasyonu: `docs/api.md`
- Gemini entegrasyonu: `docs/GEMINI_DOCUMENTATION.md`
- Teknik detaylar: `docs/GEMINI_IMPLEMENTATION.md`
