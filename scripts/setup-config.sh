#!/bin/bash

# appsettings.json dosyasını otomatik güncelleme script'i
# Kullanım: ./setup-config.sh [PROJECT_REF] [DB_PASSWORD] [SERVICE_ROLE_KEY]

set -e

PROJECT_REF=$1
DB_PASSWORD=$2
SERVICE_ROLE_KEY=$3

if [ -z "$PROJECT_REF" ] || [ -z "$DB_PASSWORD" ] || [ -z "$SERVICE_ROLE_KEY" ]; then
    echo "❌ HATA: Tüm parametreler gereklidir"
    echo ""
    echo "Kullanım:"
    echo "  ./setup-config.sh [PROJECT_REF] [DB_PASSWORD] [SERVICE_ROLE_KEY]"
    echo ""
    echo "Örnek:"
    echo "  ./setup-config.sh abcdefghijklmnop MySecurePassword123 eyJhbGci..."
    exit 1
fi

CONFIG_FILE="MentorX.API/appsettings.json"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "❌ HATA: $CONFIG_FILE dosyası bulunamadı"
    exit 1
fi

echo "📝 appsettings.json güncelleniyor..."

# Connection string oluştur
CONNECTION_STRING="Host=db.$PROJECT_REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=$DB_PASSWORD"
SUPABASE_URL="https://$PROJECT_REF.supabase.co"

# JSON dosyasını güncelle (jq kullanarak)
if command -v jq &> /dev/null; then
    jq ".ConnectionStrings.DefaultConnection = \"$CONNECTION_STRING\"" "$CONFIG_FILE" > "$CONFIG_FILE.tmp"
    jq ".Supabase.Url = \"$SUPABASE_URL\"" "$CONFIG_FILE.tmp" > "$CONFIG_FILE.tmp2"
    jq ".Supabase.ServiceRoleKey = \"$SERVICE_ROLE_KEY\"" "$CONFIG_FILE.tmp2" > "$CONFIG_FILE"
    rm "$CONFIG_FILE.tmp" "$CONFIG_FILE.tmp2"
    echo "✅ appsettings.json başarıyla güncellendi!"
else
    echo "⚠️  jq bulunamadı, manuel güncelleme gerekli"
    echo ""
    echo "Connection String: $CONNECTION_STRING"
    echo "Supabase URL: $SUPABASE_URL"
    echo "Service Role Key: $SERVICE_ROLE_KEY"
fi
