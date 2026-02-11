#!/bin/bash

# Tüm kurulum adımlarını otomatik olarak yapan script
# Kullanım: ./setup-complete.sh

set -e

echo "🚀 MentorX API - Tam Kurulum Script'i"
echo "========================================"
echo ""

# 1. Supabase projesi oluştur
if [ -z "$SUPABASE_ACCESS_TOKEN" ]; then
    echo "⚠️  SUPABASE_ACCESS_TOKEN bulunamadı"
    echo "   Supabase projesini manuel olarak oluşturmanız gerekiyor"
    echo "   Adımlar için SETUP_SUPABASE.md dosyasına bakın"
    echo ""
    read -p "Devam etmek istiyor musunuz? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
else
    echo "📦 Supabase projesi oluşturuluyor..."
    ./scripts/create-supabase-project.sh
    echo ""
fi

# 2. appsettings.json güncelle
echo "📝 appsettings.json güncelleniyor..."
if [ -f ".env" ]; then
    source .env
    if [ ! -z "$PROJECT_REF" ] && [ ! -z "$DB_PASSWORD" ] && [ ! -z "$SERVICE_ROLE_KEY" ]; then
        ./scripts/setup-config.sh "$PROJECT_REF" "$DB_PASSWORD" "$SERVICE_ROLE_KEY"
    else
        echo "⚠️  .env dosyasında eksik bilgiler var, manuel güncelleme gerekli"
    fi
else
    echo "⚠️  .env dosyası bulunamadı, manuel güncelleme gerekli"
    echo "   appsettings.json dosyasını SETUP_SUPABASE.md'deki adımlara göre güncelleyin"
fi
echo ""

# 3. Migration'ları uygula
echo "🗄️  Database migration'ları uygulanıyor..."
export PATH="$PATH:/Users/erdiacar/.dotnet/tools"
dotnet ef database update --project MentorX.Infrastructure/MentorX.Infrastructure.csproj --startup-project MentorX.API/MentorX.API.csproj
echo ""

# 4. SQL script'lerini çalıştırma talimatları
echo "📋 Sonraki Adımlar:"
echo "   1. Supabase Dashboard > SQL Editor'e gidin"
echo "   2. Şu script'leri sırayla çalıştırın:"
echo "      - scripts/sql/01-rls-policies.sql"
echo "      - scripts/sql/02-triggers.sql"
echo "      - scripts/sql/03-seed-data.sql"
echo ""

# 5. Uygulamayı çalıştır
echo "✅ Kurulum tamamlandı!"
echo ""
echo "🚀 Uygulamayı çalıştırmak için:"
echo "   cd MentorX.API"
echo "   dotnet run"
echo ""
echo "📖 Swagger UI: http://localhost:5000"
echo ""
