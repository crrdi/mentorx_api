-- Seed Data (Initial Data)
-- Bu script'i Supabase SQL Editor'de çalıştırın
-- Entity Framework table/column naming convention (PascalCase) kullanılıyor

-- MentorRole seed data
INSERT INTO public."MentorRoles" ("Id", "Code", "DisplayName", "CreatedAt", "UpdatedAt")
VALUES 
  ('00000000-0000-0000-0000-000000000001', 'MENTOR', 'Mentor', NOW(), NOW()),
  ('00000000-0000-0000-0000-000000000002', 'EXPERT', 'Expert', NOW(), NOW()),
  ('00000000-0000-0000-0000-000000000003', 'COACH', 'Coach', NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

-- CreditPackages seed data (RevenueCatProductId links to App Store/Play Store product)
INSERT INTO public."CreditPackages" ("Id", "Name", "Credits", "Price", "BonusPercentage", "Badge", "RevenueCatProductId", "Type", "CreatedAt", "UpdatedAt")
VALUES 
  (gen_random_uuid(), 'Starter', 10, 4.99, NULL, NULL, NULL, 'one_time', NOW(), NOW()),
  (gen_random_uuid(), 'Popular', 25, 9.99, 25, 'POPULAR', NULL, 'one_time', NOW(), NOW()),
  (gen_random_uuid(), 'Pro', 50, 17.99, 30, 'BEST VALUE', NULL, 'one_time', NOW(), NOW()),
  (gen_random_uuid(), 'Enterprise', 100, 29.99, 50, NULL, 'com.erdiacar.mentorx.credits_100', 'one_time', NOW(), NOW())
ON CONFLICT DO NOTHING;
