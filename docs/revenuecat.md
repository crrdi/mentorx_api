# Backend Agent Prompt: RevenueCat Webhook Entegrasyonu

Bu doküman, RevenueCat webhook entegrasyonu için backend tarafında yapılması gerekenleri özetler.

---

## Değişiklik Özeti

**Mevcut durum:** Credit paketleri backend üzerinden satın alınıyor (`POST /api/credits/purchase`).

**Yeni durum:** RevenueCat ile native in-app purchase desteği. Satın alma işlemleri RevenueCat üzerinden yapılacak, backend RevenueCat webhook'ları ile bilgilendirilecek ve kullanıcının subscription durumunu ve credits'ini güncelleyecek.

---

## 1. Database Schema Güncellemesi

### 1.1 Users Tablosuna Subscription Alanları Ekleme

**Değişiklik:**

```sql
ALTER TABLE Users
ADD COLUMN revenueCatCustomerId VARCHAR(255) NULL,
ADD COLUMN subscriptionStatus VARCHAR(50) NULL CHECK (subscriptionStatus IN ('none', 'active', 'expired', 'cancelled')),
ADD COLUMN subscriptionProductId VARCHAR(255) NULL,
ADD COLUMN subscriptionExpiresAt TIMESTAMP NULL;

CREATE INDEX idx_users_revenuecat_customer ON Users(revenueCatCustomerId);
CREATE INDEX idx_users_subscription_status ON Users(subscriptionStatus);
```

**Alan Açıklamaları:**

- `revenueCatCustomerId`: RevenueCat'in kullanıcıya atadığı unique customer ID
- `subscriptionStatus`: Subscription durumu ('none', 'active', 'expired', 'cancelled')
- `subscriptionProductId`: Aktif subscription'ın product ID'si (örn: "premium_monthly")
- `subscriptionExpiresAt`: Subscription'ın bitiş tarihi (subscription'lar için)

**Not:** Eğer tablo isimleri snake_case ise (`users`, `revenue_cat_customer_id`, vb.) buna göre uyarlayın.

---

## 2. POST /api/webhooks/revenuecat

**Endpoint:** `POST /api/webhooks/revenuecat`

**Webhook URL:** `https://mentorx-api-gr2ceodgsq-uc.a.run.app/api/webhooks/revenuecat`

**Authentication:** ❌ Standart Bearer token kullanılmaz. RevenueCat dashboard'da tanımladığınız **Authorization header** (örn: `Bearer {secret}`) ile doğrulanır.

> **Önemli:** RevenueCat kriptografik imza kullanmaz. Sadece dashboard'da ayarladığınız Authorization header değeri ile doğrulama yapılır. Webhook URL'si ve secret RevenueCat Pro planında mevcuttur.

**Request Headers:**

```
Authorization: Bearer {revenuecat_webhook_secret}
```

**Request Body (Resmi Format - `customer_info` YOK):**

RevenueCat webhook payload'ı sadece `event` ve `api_version` içerir. `customer_info` alanı **gönderilmez**.

```json
{
  "event": {
    "id": "12345678-1234-1234-1234-123456789012",
    "type": "INITIAL_PURCHASE",
    "app_user_id": "550e8400-e29b-41d4-a716-446655440000",
    "original_app_user_id": "550e8400-e29b-41d4-a716-446655440000",
    "aliases": ["$RCAnonymousID:8069238d6049ce87cc529853916d624c"],
    "product_id": "premium_monthly",
    "period_type": "NORMAL",
    "purchased_at_ms": 1707652800000,
    "expiration_at_ms": 1710331200000,
    "environment": "PRODUCTION",
    "entitlement_ids": ["premium"],
    "transaction_id": "transaction_123",
    "store": "APP_STORE"
  },
  "api_version": "1.0"
}
```

> **app_user_id:** Flutter/RevenueCat SDK'da `Purchases.configure(appUserID: supabaseUserId)` ile kendi User.Id (Guid) kullanın. Böylece webhook'ta kullanıcı eşleşmesi doğrudan yapılır.

**Business Logic:**

1. **Webhook Doğrulama:**
   - RevenueCat dashboard'da tanımladığınız secret'ı `RevenueCat:WebhookSecret` veya `REVENUECAT_WEBHOOK_SECRET` env var'dan alın
   - `Authorization: Bearer {secret}` header'ını karşılaştırın (kriptografik imza yoktur)

2. **User Lookup:** RevenueCat dokümantasyonuna göre `app_user_id`, `original_app_user_id` ve `aliases` dizisindeki tüm ID'ler aranmalıdır. Supabase User.Id (Guid) kullanıyorsanız bu ID'lerden biri eşleşir.

3. **Event Type'a Göre İşlem:**

   **INITIAL_PURCHASE / RENEWAL / NON_RENEWING_PURCHASE:**
   - User'ı `app_user_id`, `original_app_user_id` veya `aliases` ile bul
   - Subscription aktif yap: `subscriptionStatus = 'active'`
   - `subscriptionProductId` ve `subscriptionExpiresAt` güncelle
   - Credits ekle (product'a göre - product mapping'i yapılmalı)

   **RENEWAL:**
   - User'ı bul
   - `subscriptionExpiresAt` güncelle
   - Credits ekle (yenileme bonusu varsa)

   **CANCELLATION:**
   - `subscriptionStatus = 'cancelled'`
   - `subscriptionExpiresAt` değişmez (mevcut süre bitene kadar aktif)

   **UNCANCELLATION:**
   - `subscriptionStatus = 'active'`
   - Subscription devam eder

   **EXPIRATION:**
   - `subscriptionStatus = 'expired'`
   - Credits ekleme durdurulur

   **BILLING_ISSUE:**
   - Log'a kaydet (kullanıcıya bildirim gönderilebilir)

   **SUBSCRIPTION_PAUSED:** (Sadece Play Store)
   - `subscriptionStatus = 'paused'` - Access henüz kaldırılmaz, sadece EXPIRATION geldiğinde kaldırılır

   **SUBSCRIPTION_EXTENDED:**
   - `subscriptionExpiresAt` güncelle

3. **Idempotency Kontrolü:**
   - Aynı `event.id` ile gelen event'i tekrar işleme
   - Event ID'yi cache'le veya database'de sakla

4. **Credits Ekleme:**
   - Product ID'ye göre credits miktarını belirle (product mapping)
   - `Users.credits` alanını güncelle
   - (Optional) `CreditTransactions` tablosuna kayıt ekle

**Response (200 OK):**

```json
{
  "success": true,
  "processed": true
}
```

**Error Cases:**

- `401`: Webhook signature geçersiz
- `400`: Geçersiz request body
- `404`: User bulunamadı (`app_user_id` ile)
- `500`: Internal server error

**Diğer Event Tipleri:** `TRANSFER`, `PRODUCT_CHANGE`, `REFUND_REVERSED`, `BILLING_ISSUE` - implementasyonda desteklenir.

**Database İşlemleri Örneği:**

```sql
-- User bulma: app_user_id, original_app_user_id veya aliases içindeki Guid ile
SELECT * FROM Users WHERE id = ?::uuid;

-- Subscription güncelleme
UPDATE Users
SET
  revenueCatCustomerId = ?,
  subscriptionStatus = ?,
  subscriptionProductId = ?,
  subscriptionExpiresAt = ?,
  credits = credits + ?,
  updatedAt = NOW()
WHERE id = ?;

-- Event ID kontrolü (idempotency)
-- Event ID'yi bir tabloda saklayabilirsiniz veya cache kullanabilirsiniz
```

---

## 3. POST /api/users/me/revenuecat-customer

**Endpoint:** `POST /api/users/me/revenuecat-customer`

**Authentication:** ✅ Required

**Request:**

```json
{
  "customerId": "revenuecat_customer_id_123"
}
```

**Request Model:**
| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `customerId` | string | ✅ | Non-empty | RevenueCat customer ID |

**Response (200 OK):**

```json
{
  "success": true,
  "customerId": "revenuecat_customer_id_123"
}
```

**Business Logic:**

1. Token'dan userId çıkarılır
2. User bulunur
3. `revenueCatCustomerId` güncellenir
4. Response döner

**Database İşlemleri:**

```sql
UPDATE Users
SET revenueCatCustomerId = ?, updatedAt = NOW()
WHERE id = ? AND deletedAt IS NULL;
```

**Error Cases:**

- `401`: Token eksik veya geçersiz
- `400`: `customerId` eksik veya geçersiz
- `404`: User bulunamadı

---

## 4. GET /api/users/me/subscription

**Endpoint:** `GET /api/users/me/subscription`

**Authentication:** ✅ Required

**Response (200 OK):**

```json
{
  "status": "active",
  "productId": "premium_monthly",
  "expiresAt": "2026-03-11T10:00:00Z",
  "isActive": true,
  "customerId": "revenuecat_customer_id_123"
}
```

**Business Logic:**

1. Token'dan userId çıkarılır
2. User'ın subscription bilgileri getirilir
3. `isActive`: `subscriptionStatus = 'active'` ve `subscriptionExpiresAt > NOW()`

**Database İşlemleri:**

```sql
SELECT
  subscriptionStatus,
  subscriptionProductId,
  subscriptionExpiresAt,
  revenueCatCustomerId
FROM Users
WHERE id = ? AND deletedAt IS NULL;
```

**Error Cases:**

- `401`: Token eksik veya geçersiz
- `404`: User bulunamadı

**Not:** İsteğe bağlı olarak RevenueCat API'den güncel bilgi çekilebilir, ancak bu endpoint database'den döner (daha hızlı).

---

## 5. GET /api/credits/packages Güncellemesi

**Endpoint:** `GET /api/credits/packages`

**Değişiklik:** Response'a `revenueCatProductId` ve `type` alanları eklenecek.

**Response (200 OK):**

```json
[
  {
    "id": "package_1",
    "name": "Starter",
    "credits": 10,
    "price": 4.99,
    "revenueCatProductId": "starter_pack",
    "type": "one_time",
    "badge": null,
    "bonusPercentage": null
  },
  {
    "id": "package_2",
    "name": "Premium Monthly",
    "credits": 100,
    "price": 9.99,
    "revenueCatProductId": "premium_monthly",
    "type": "subscription",
    "badge": "POPULAR",
    "bonusPercentage": null
  }
]
```

**Yeni Alanlar:**

- `revenueCatProductId`: RevenueCat'teki product ID (Flutter'da purchase için kullanılacak)
- `type`: `"one_time"` veya `"subscription"`

**Business Logic:**

- Paketler hardcoded olabilir veya `CreditPackages` tablosundan çekilebilir
- Her paket için RevenueCat product ID eşleştirmesi yapılmalı

---

## 6. Product ID Mapping

RevenueCat product ID'leri ile credit miktarlarını eşleştirmek için bir mapping gereklidir.

**Örnek Mapping:**

```javascript
const PRODUCT_CREDIT_MAPPING = {
  starter_pack: { credits: 10, type: "one_time" },
  popular_pack: { credits: 25, type: "one_time" },
  pro_pack: { credits: 50, type: "one_time" },
  premium_monthly: { credits: 100, type: "subscription", monthly: true },
  premium_annual: { credits: 1200, type: "subscription", monthly: false },
};
```

Bu mapping webhook handler'da kullanılacak:

- Webhook'tan gelen `product_id` ile credits miktarını bul
- User'a credits ekle

**Not:** Bu mapping bir config dosyasında, database'de veya environment variable'da tutulabilir.

---

## 7. Environment Variables

Backend'e eklenecek environment variables:

```env
REVENUECAT_SECRET_KEY={secret_key}
REVENUECAT_WEBHOOK_SECRET={webhook_secret}
REVENUECAT_PROJECT_ID={project_id}
```

**Not:** `REVENUECAT_WEBHOOK_SECRET` webhook signature doğrulaması için kullanılır.

---

## 8. Error Handling & Logging

**Webhook Handler İçin:**

- Tüm webhook event'lerini log'la (audit trail için)
- Hata durumlarında RevenueCat'e retry için uygun response dön
- Failed event'leri queue'ya al (retry mekanizması için)

**Log Formatı:**

```
[RevenueCat Webhook] Event: {event_type}, User: {user_id}, Product: {product_id}, Status: {status}
```

---

## 9. Testing

**Test Senaryoları:**

1. **INITIAL_PURCHASE:**
   - Yeni kullanıcı purchase yapar
   - Webhook gelir, user'ın subscription'ı aktif olur
   - Credits eklenir

2. **RENEWAL:**
   - Subscription yenilenir
   - Credits tekrar eklenir
   - `subscriptionExpiresAt` güncellenir

3. **CANCELLATION:**
   - Subscription iptal edilir
   - `subscriptionStatus = 'cancelled'`
   - Mevcut süre bitene kadar aktif kalır

4. **EXPIRATION:**
   - Subscription süresi biter
   - `subscriptionStatus = 'expired'`
   - Credits ekleme durur

5. **Idempotency:**
   - Aynı event ID ile iki kez webhook gelirse
   - İkinci çağrı ignore edilir

---

## 10. Migration Script

Database migration script'i:

```sql
-- Add subscription columns to Users table
ALTER TABLE Users
ADD COLUMN revenueCatCustomerId VARCHAR(255) NULL,
ADD COLUMN subscriptionStatus VARCHAR(50) NULL,
ADD COLUMN subscriptionProductId VARCHAR(255) NULL,
ADD COLUMN subscriptionExpiresAt TIMESTAMP NULL;

-- Add check constraint for subscriptionStatus
ALTER TABLE Users
ADD CONSTRAINT chk_subscription_status
CHECK (subscriptionStatus IN ('none', 'active', 'expired', 'cancelled') OR subscriptionStatus IS NULL);

-- Create indexes
CREATE INDEX idx_users_revenuecat_customer ON Users(revenueCatCustomerId);
CREATE INDEX idx_users_subscription_status ON Users(subscriptionStatus);

-- Set default values for existing users
UPDATE Users
SET subscriptionStatus = 'none'
WHERE subscriptionStatus IS NULL;
```

---

## Implementation Durumu

Bu endpoint'ler implement edilmiştir:

- ✅ `POST /api/webhooks/revenuecat` - Webhook handler (AllowAnonymous, Bearer secret doğrulama)
- ✅ `POST /api/users/me/revenuecat-customer` - Customer ID linking
- ✅ `GET /api/users/me/subscription` - Subscription durumu
- ✅ `GET /api/credits/packages` - `revenueCatProductId` ve `type` alanları eklendi
- ✅ Database migration: User subscription alanları + RevenueCatWebhookEvents (idempotency)
- ✅ Credits lookup: `CreditPackages` tablosundan `RevenueCatProductId` ile (tek kaynak DB)

**Konfigürasyon:**
- `appsettings.json` veya `REVENUECAT_WEBHOOK_SECRET` env variable
- RevenueCat dashboard: Integrations > Webhooks
  - Webhook URL: `https://mentorx-api-gr2ceodgsq-uc.a.run.app/api/webhooks/revenuecat`
  - Authorization header: `Bearer your_secret`

**Flutter SDK:** `Purchases.configure(apiKey: "...", appUserID: supabaseUser.id)` ile User.Id kullanın.

---

**Son Güncelleme:** 2026-02-11
