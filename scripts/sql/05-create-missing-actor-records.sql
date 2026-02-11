-- Migration: Create Missing Actor Records
-- Bu script eksik actor kayıtlarını oluşturur
-- User ve Mentor kayıtları için actor kayıtları kontrol edilir ve eksik olanlar oluşturulur

-- 1. User'lar için eksik actor kayıtlarını oluştur
INSERT INTO "Actors" ("Id", "Type", "UserId", "MentorId", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    1,  -- ActorType.User = 1
    u."Id",
    NULL,
    COALESCE(u."CreatedAt", NOW()),
    COALESCE(u."UpdatedAt", NOW())
FROM "Users" u
WHERE NOT EXISTS (
    SELECT 1 FROM "Actors" a 
    WHERE a."UserId" = u."Id" AND a."Type" = 1
)
ON CONFLICT DO NOTHING;

-- 2. Mentor'lar için eksik actor kayıtlarını oluştur
INSERT INTO "Actors" ("Id", "Type", "UserId", "MentorId", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    2,  -- ActorType.Mentor = 2
    NULL,
    m."Id",
    COALESCE(m."CreatedAt", NOW()),
    COALESCE(m."UpdatedAt", NOW())
FROM "Mentors" m
WHERE NOT EXISTS (
    SELECT 1 FROM "Actors" a 
    WHERE a."MentorId" = m."Id" AND a."Type" = 2
)
ON CONFLICT DO NOTHING;

-- 3. Sonuç kontrolü
SELECT 
    'Users' as table_name,
    COUNT(*) as total_records,
    COUNT(a."Id") as records_with_actors,
    COUNT(*) - COUNT(a."Id") as records_without_actors
FROM "Users" u
LEFT JOIN "Actors" a ON a."UserId" = u."Id" AND a."Type" = 1

UNION ALL

SELECT 
    'Mentors' as table_name,
    COUNT(*) as total_records,
    COUNT(a."Id") as records_with_actors,
    COUNT(*) - COUNT(a."Id") as records_without_actors
FROM "Mentors" m
LEFT JOIN "Actors" a ON a."MentorId" = m."Id" AND a."Type" = 2;
