#!/bin/bash

# Supabase Projesi Oluşturma Script'i
# Kullanım: ./create-supabase-project.sh

set -e

echo "🚀 MentorX Supabase Projesi Oluşturma"
echo "========================================"
echo ""

# Access token kontrolü
if [ -z "$SUPABASE_ACCESS_TOKEN" ]; then
    echo "❌ HATA: SUPABASE_ACCESS_TOKEN environment variable'ı ayarlanmamış"
    echo ""
    echo "Access token'ı şu adresten alabilirsiniz:"
    echo "https://supabase.com/dashboard/account/tokens"
    echo ""
    echo "Kullanım:"
    echo "  export SUPABASE_ACCESS_TOKEN='your-access-token'"
    echo "  ./create-supabase-project.sh"
    exit 1
fi

# Organizasyon ID'sini al
echo "📋 Organizasyonlar listeleniyor..."
ORGS=$(curl -s -H "Authorization: Bearer $SUPABASE_ACCESS_TOKEN" \
  https://api.supabase.com/v1/organizations)

ORG_COUNT=$(echo $ORGS | jq '. | length')
if [ "$ORG_COUNT" -eq 0 ]; then
    echo "❌ HATA: Hiç organizasyon bulunamadı"
    exit 1
fi

echo "Bulunan organizasyonlar:"
echo $ORGS | jq -r '.[] | "  - \(.name) (ID: \(.id))"'
echo ""

# İlk organizasyonu kullan (veya kullanıcıdan al)
ORG_ID=$(echo $ORGS | jq -r '.[0].id')
ORG_NAME=$(echo $ORGS | jq -r '.[0].name')
echo "✅ Organizasyon seçildi: $ORG_NAME (ID: $ORG_ID)"
echo ""

# Proje bilgileri
PROJECT_NAME="MentorX"
REGION="us-east-1"  # Veya size yakın bir region

# Database şifresi oluştur
DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
echo "🔐 Database şifresi oluşturuldu: $DB_PASSWORD"
echo "⚠️  LÜTFEN BU ŞİFREYİ KAYDEDİN!"
echo ""

# Proje oluştur
echo "📦 Supabase projesi oluşturuluyor..."
RESPONSE=$(curl -s -X POST https://api.supabase.com/v1/projects \
  -H "Authorization: Bearer $SUPABASE_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"organization_id\": \"$ORG_ID\",
    \"name\": \"$PROJECT_NAME\",
    \"region\": \"$REGION\",
    \"db_pass\": \"$DB_PASSWORD\"
  }")

PROJECT_ID=$(echo $RESPONSE | jq -r '.id // empty')
PROJECT_REF=$(echo $RESPONSE | jq -r '.ref // empty')

if [ -z "$PROJECT_ID" ] || [ "$PROJECT_ID" == "null" ]; then
    echo "❌ HATA: Proje oluşturulamadı"
    echo "Response: $RESPONSE"
    exit 1
fi

echo "✅ Proje oluşturuldu!"
echo "   Project ID: $PROJECT_ID"
echo "   Project Ref: $PROJECT_REF"
echo ""

# Proje hazır olana kadar bekle
echo "⏳ Proje hazır olana kadar bekleniyor (bu 1-2 dakika sürebilir)..."
for i in {1..60}; do
    STATUS=$(curl -s -H "Authorization: Bearer $SUPABASE_ACCESS_TOKEN" \
      "https://api.supabase.com/v1/projects/$PROJECT_ID" | jq -r '.status // empty')
    
    if [ "$STATUS" == "ACTIVE_HEALTHY" ]; then
        echo "✅ Proje hazır!"
        break
    fi
    
    if [ $i -eq 60 ]; then
        echo "⚠️  UYARI: Proje henüz hazır değil, lütfen dashboard'dan kontrol edin"
    else
        echo -n "."
        sleep 2
    fi
done

echo ""
echo ""

# API keys'i al
echo "🔑 API keys alınıyor..."
KEYS_RESPONSE=$(curl -s -H "Authorization: Bearer $SUPABASE_ACCESS_TOKEN" \
  "https://api.supabase.com/v1/projects/$PROJECT_ID/api-keys")

ANON_KEY=$(echo $KEYS_RESPONSE | jq -r '.[] | select(.name == "anon") | .api_key')
SERVICE_ROLE_KEY=$(echo $KEYS_RESPONSE | jq -r '.[] | select(.name == "service_role") | .api_key')

if [ -z "$ANON_KEY" ] || [ -z "$SERVICE_ROLE_KEY" ]; then
    echo "⚠️  UYARI: API keys alınamadı, lütfen dashboard'dan manuel olarak alın"
else
    echo "✅ API keys alındı"
fi

echo ""
echo "========================================"
echo "✅ Supabase Projesi Başarıyla Oluşturuldu!"
echo "========================================"
echo ""
echo "📝 Proje Bilgileri:"
echo "   Project Name: $PROJECT_NAME"
echo "   Project Ref: $PROJECT_REF"
echo "   Project URL: https://$PROJECT_REF.supabase.co"
echo "   Database Password: $DB_PASSWORD"
echo ""
echo "🔑 API Keys:"
echo "   Anon Key: $ANON_KEY"
echo "   Service Role Key: $SERVICE_ROLE_KEY"
echo ""
echo "📋 Sonraki Adımlar:"
echo "   1. Bu bilgileri appsettings.json dosyasına ekleyin"
echo "   2. Migration'ları uygulayın: dotnet ef database update"
echo "   3. SQL script'lerini çalıştırın (scripts/sql/ klasöründeki dosyalar)"
echo ""
echo "💾 Bu bilgileri kaydetmek için:"
echo "   echo 'PROJECT_REF=$PROJECT_REF' >> .env"
echo "   echo 'DB_PASSWORD=$DB_PASSWORD' >> .env"
echo "   echo 'SERVICE_ROLE_KEY=$SERVICE_ROLE_KEY' >> .env"
echo ""
