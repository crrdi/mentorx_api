-- RevenueCat Subscription ve Webhook Entegrasyonu
-- Bu script'i migration çalışmazsa manuel olarak çalıştırın.

-- Users tablosuna RevenueCat alanları
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "RevenueCatCustomerId" VARCHAR(255) NULL,
ADD COLUMN IF NOT EXISTS "SubscriptionStatus" VARCHAR(50) NULL,
ADD COLUMN IF NOT EXISTS "SubscriptionProductId" VARCHAR(255) NULL,
ADD COLUMN IF NOT EXISTS "SubscriptionExpiresAt" TIMESTAMP WITH TIME ZONE NULL;

-- İndeksler
CREATE INDEX IF NOT EXISTS "IX_Users_RevenueCatCustomerId" ON "Users" ("RevenueCatCustomerId");
CREATE INDEX IF NOT EXISTS "IX_Users_SubscriptionStatus" ON "Users" ("SubscriptionStatus");

-- Idempotency için webhook event tablosu
CREATE TABLE IF NOT EXISTS "RevenueCatWebhookEvents" (
    "EventId" VARCHAR(255) NOT NULL PRIMARY KEY,
    "ProcessedAt" TIMESTAMP WITH TIME ZONE NOT NULL
);
