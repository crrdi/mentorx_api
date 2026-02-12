-- Mentor Avatars Storage Bucket
-- Bu script'i Supabase SQL Editor'de çalıştırın
-- Mentor oluşturulduğunda Gemini ile otomatik üretilen avatar'lar bu bucket'a yüklenir
-- Bucket zaten varsa "duplicate key" hatası alabilirsiniz - güvenle yoksayın

INSERT INTO storage.buckets (id, name, public)
VALUES ('mentor-avatars', 'mentor-avatars', true);
