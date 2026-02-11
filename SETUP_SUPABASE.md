# Supabase Kurulum Rehberi

Bu rehber MentorX API projesi için Supabase kurulumunu adım adım açıklar.

## Adım 1: Supabase Projesi Oluşturma

### Web Dashboard ile (Önerilen)

1. **Supabase'e giriş yapın**
   - [https://supabase.com/dashboard](https://supabase.com/dashboard) adresine gidin
   - Hesabınız yoksa ücretsiz kayıt olun

2. **Yeni proje oluşturun**
   - "New Project" butonuna tıklayın
   - Formu doldurun:
     - **Name**: `MentorX`
     - **Database Password**: Güçlü bir şifre belirleyin ve **mutlaka kaydedin**
     - **Region**: Size en yakın bölgeyi seçin (örn: `West US (North California)`)
   - "Create new project" butonuna tıklayın
   - Proje oluşturulması 1-2 dakika sürebilir

3. **API Bilgilerini Alın**
   - Proje oluşturulduktan sonra **Settings** > **API** sayfasına gidin
   - Şu bilgileri kopyalayın:
     - **Project URL**: `https://xxxxx.supabase.co`
     - **anon public** key: `eyJhbGci...` (anon key)
     - **service_role** key: `eyJhbGci...` (service_role key - gizli tutun!)

## Adım 2: Database Connection String

1. **Settings** > **Database** sayfasına gidin
2. **Connection string** bölümünde **URI** formatını seçin
3. Connection string şu formatta olacak:
   ```
   postgresql://postgres:[YOUR-PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres
   ```
4. `[YOUR-PASSWORD]` kısmını proje oluştururken belirlediğiniz şifreyle değiştirin

## Adım 3: appsettings.json Güncelleme

`MentorX.API/appsettings.json` dosyasını açın ve şu değerleri güncelleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres:[YOUR-PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres"
  },
  "Supabase": {
    "Url": "https://[PROJECT-REF].supabase.co",
    "ServiceRoleKey": "eyJhbGci...service_role_key_here"
  }
}
```

**Örnek:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres:MySecurePassword123@db.abcdefghijklmnop.supabase.co:5432/postgres"
  },
  "Supabase": {
    "Url": "https://abcdefghijklmnop.supabase.co",
    "ServiceRoleKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImFiY2RlZmdoaWprbG1ub3AiLCJyb2xlIjoic2VydmljZV9yb2xlIiwiaWF0IjoxNjE2MjM5MDIyfQ.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

## Adım 4: Database Migration Uygulama

Terminal'de proje kök dizininde şu komutu çalıştırın:

```bash
# PATH'e dotnet-ef tools ekleyin (ilk seferinde)
export PATH="$PATH:/Users/erdiacar/.dotnet/tools"

# Migration'ları uygulayın
dotnet ef database update --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj
```

Bu komut:
- Tüm tabloları oluşturur
- İlişkileri kurar
- Index'leri ekler
- Soft delete query filter'larını yapılandırır

## Adım 5: Supabase Auth Yapılandırması

### Email Auth Aktifleştirme

1. **Authentication** > **Providers** sayfasına gidin
2. **Email** provider'ının aktif olduğundan emin olun
3. **Confirm email** ayarını ihtiyacınıza göre yapılandırın:
   - **Aktif**: Kullanıcılar email doğrulaması yapmalı
   - **Pasif**: Direkt giriş yapabilirler

### OAuth Provider'lar

**Google:** Authentication > Providers > Google — Client ID ve Client Secret ekleyin (Google Cloud Console'dan).

**Apple:** Authentication > Providers > Apple — Aşağıdakileri doldurun:
1. **Apple enabled** = ON
2. **Services ID** (örn. `com.yourapp.signin`) — Apple Developer'da oluşturulur
3. **Secret Key** — Apple Developer'da Key oluşturup indirdiğiniz `.p8` dosyasının içeriği
4. **Key ID** — Key'in ID'si
5. **Team ID** — Apple Developer Team ID (10 karakter)
6. **Bundle ID** (iOS app için) — Xcode'daki bundle identifier

Apple yapılandırması yoksa veya yanlışsa `POST /api/auth/apple` **400** döner. Hata mesajı yanıtta `error` alanında gelir; loglarda da görünür.

## Adım 6: Row Level Security (RLS) Politikaları

Supabase Dashboard'dan SQL Editor'ü kullanarak RLS politikalarını ekleyin:

1. **SQL Editor** sayfasına gidin
2. `docs/api.md` dosyasındaki RLS policy örneklerini kullanarak politikaları oluşturun

**Örnek (Users tablosu için):**
```sql
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can read own profile"
ON public.users
FOR SELECT
USING (auth.uid() = id AND "deletedAt" IS NULL);

CREATE POLICY "Users can update own profile"
ON public.users
FOR UPDATE
USING (auth.uid() = id)
WITH CHECK (auth.uid() = id);
```

## Adım 7: Database Trigger'ları (Opsiyonel)

Yeni kullanıcı oluşturulduğunda otomatik olarak `users` ve `actors` tablolarına kayıt eklemek için trigger ekleyin:

```sql
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.users (
    id, email, name, avatar, "createdAt", "updatedAt", 
    "deletedAt", "focusAreas", credits
  )
  VALUES (
    NEW.id,
    NEW.email,
    COALESCE(NEW.raw_user_meta_data->>'name', 'User'),
    NULL,
    NOW(),
    NOW(),
    NULL,
    '[]'::text[],
    10
  );

  INSERT INTO public.actors (id, type, "userId", "mentorId", "createdAt", "updatedAt")
  VALUES (
    gen_random_uuid(),
    'user',
    NEW.id,
    NULL,
    NOW(),
    NOW()
  );

  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
```

## Adım 8: Test

1. Uygulamayı çalıştırın:
   ```bash
   cd MentorX.API
   dotnet run
   ```

2. Swagger UI'ı açın: `http://localhost:5000`

3. İlk kullanıcıyı kaydedin:
   - `POST /api/auth/register` endpoint'ini kullanın
   - Email ve password gönderin

4. Supabase Dashboard'da kontrol edin:
   - **Authentication** > **Users**: Yeni kullanıcı görünmeli
   - **Table Editor** > **users**: Kullanıcı profili görünmeli
   - **Table Editor** > **actors**: Actor kaydı görünmeli

## Sorun Giderme

### Connection String Hatası
- Şifrenin doğru olduğundan emin olun
- Connection string formatını kontrol edin
- Supabase projesinin aktif olduğundan emin olun

### Migration Hatası
- Database'in boş olduğundan emin olun
- Connection string'in doğru olduğundan emin olun
- `dotnet ef` tools'un yüklü olduğundan emin olun

### Auth Hatası
- Service Role Key'in doğru olduğundan emin olun
- Supabase URL'in doğru olduğundan emin olun
- Email provider'ın aktif olduğundan emin olun

## Güvenlik Uyarıları

⚠️ **ÖNEMLİ:**
- Service Role Key'i asla client-side kodda kullanmayın
- Service Role Key'i Git'e commit etmeyin
- Production'da environment variable kullanın
- Database password'ü güvenli tutun

## Sonraki Adımlar

1. ✅ Supabase projesi oluşturuldu
2. ✅ Database migration'ları uygulandı
3. ✅ appsettings.json güncellendi
4. ⏭️ RLS politikalarını ekleyin
5. ⏭️ Database trigger'larını ekleyin (opsiyonel)
6. ⏭️ OAuth provider'ları yapılandırın (opsiyonel)
