# Google Cloud Run Deployment Guide

Bu doküman MentorX API projesini Google Cloud Run'a deploy etmek için gerekli adımları açıklar.

## Önkoşullar

1. **Google Cloud Account**: [Google Cloud Console](https://console.cloud.google.com/) hesabınız olmalı
2. **Google Cloud CLI**: `gcloud` CLI kurulu olmalı
3. **Docker**: Docker kurulu olmalı (local test için)
4. **Billing**: Google Cloud projenizde billing aktif olmalı

## Adım 1: Google Cloud Projesi Oluşturma

1. [Google Cloud Console](https://console.cloud.google.com/) → **Create Project**
2. Proje adını girin: `mentorx-api` (veya istediğiniz isim)
3. Proje ID'yi not edin (örn: `mentorx-api-123456`)

## Adım 2: Gerekli API'leri Aktifleştirme

```bash
# Cloud Run API
gcloud services enable run.googleapis.com

# Container Registry API
gcloud services enable containerregistry.googleapis.com

# Cloud Build API (opsiyonel, GitHub Actions için)
gcloud services enable cloudbuild.googleapis.com
```

## Adım 3: Service Account Oluşturma

GitHub Actions için bir service account oluşturun:

```bash
# Service account oluştur
gcloud iam service-accounts create github-actions \
    --display-name="GitHub Actions Service Account"

# Gerekli roller ver
gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
    --member="serviceAccount:github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/run.admin"

gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
    --member="serviceAccount:github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/storage.admin"

gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
    --member="serviceAccount:github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/iam.serviceAccountUser"

# Service account key oluştur
gcloud iam service-accounts keys create key.json \
    --iam-account=github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com
```

## Adım 4: GitHub Secrets Ekleme

GitHub repository'nize şu secret'ları ekleyin:

1. **GCP_PROJECT_ID**: Google Cloud proje ID'niz
2. **GCP_SA_KEY**: Service account key JSON içeriği (key.json dosyasının tamamı)

### GitHub'a Secret Ekleme:

1. Repository → **Settings** → **Secrets and variables** → **Actions**
2. **New repository secret** → Şu secret'ları ekleyin:
   - `GCP_PROJECT_ID`: Proje ID'niz
   - `GCP_SA_KEY`: Service account key JSON içeriği

## Adım 5: Local Test (Opsiyonel)

Docker image'ı local'de test edebilirsiniz:

```bash
# Docker image build et
docker build -t mentorx-api .

# Local'de çalıştır
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING" \
  -e Supabase__Url="YOUR_SUPABASE_URL" \
  -e Supabase__AnonKey="YOUR_ANON_KEY" \
  -e Supabase__ServiceRoleKey="YOUR_SERVICE_ROLE_KEY" \
  -e Gemini__ApiKey="YOUR_GEMINI_API_KEY" \
  -e RevenueCat__WebhookSecret="YOUR_WEBHOOK_SECRET" \
  mentorx-api
```

## Adım 6: Manuel Deploy

GitHub Actions kullanmadan manuel deploy:

```bash
# Google Cloud'a authenticate ol
gcloud auth login
gcloud config set project YOUR_PROJECT_ID

# Docker image build ve push
docker build -t gcr.io/YOUR_PROJECT_ID/mentorx-api:latest .
docker push gcr.io/YOUR_PROJECT_ID/mentorx-api:latest

# Cloud Run'a deploy
gcloud run deploy mentorx-api \
  --image gcr.io/YOUR_PROJECT_ID/mentorx-api:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars="ConnectionStrings__DefaultConnection=YOUR_CONNECTION_STRING" \
  --set-env-vars="Supabase__Url=YOUR_SUPABASE_URL" \
  --set-env-vars="Supabase__AnonKey=YOUR_ANON_KEY" \
  --set-env-vars="Supabase__ServiceRoleKey=YOUR_SERVICE_ROLE_KEY" \
  --set-env-vars="Gemini__ApiKey=YOUR_GEMINI_API_KEY" \
  --set-env-vars="RevenueCat__WebhookSecret=YOUR_WEBHOOK_SECRET" \
  --set-env-vars="ASPNETCORE_ENVIRONMENT=Production" \
  --memory=512Mi \
  --cpu=1 \
  --timeout=300 \
  --max-instances=10
```

## Adım 7: Otomatik Deploy (GitHub Actions)

GitHub Actions workflow'u otomatik olarak deploy edecek:

1. `main` branch'ine push yaptığınızda otomatik deploy başlar
2. Workflow'u manuel olarak da çalıştırabilirsiniz: **Actions** → **Deploy to Google Cloud Run** → **Run workflow**

## Environment Variables

Cloud Run'da şu environment variable'lar otomatik olarak set edilir:

- `ConnectionStrings__DefaultConnection`
- `Supabase__Url`
- `Supabase__AnonKey`
- `Supabase__ServiceRoleKey`
- `Gemini__ApiKey`
- `RevenueCat__WebhookSecret`
- `ASPNETCORE_ENVIRONMENT=Production`
- `PORT` (Cloud Run otomatik set eder)

## Cloud Run Ayarları

- **Memory**: 512Mi (artırılabilir)
- **CPU**: 1 vCPU
- **Timeout**: 300 saniye (5 dakika)
- **Max Instances**: 10 (otomatik scaling)
- **Min Instances**: 0 (cold start olabilir)

## Service URL

Deploy sonrası service URL'i almak için:

```bash
gcloud run services describe mentorx-api \
  --platform managed \
  --region us-central1 \
  --format 'value(status.url)'
```

## Troubleshooting

### "Permission denied" Hatası

Service account'a gerekli roller verildiğinden emin olun:

```bash
gcloud projects get-iam-policy YOUR_PROJECT_ID \
  --flatten="bindings[].members" \
  --filter="bindings.members:serviceAccount:github-actions@YOUR_PROJECT_ID.iam.gserviceaccount.com"
```

### "Image not found" Hatası

Docker image'ın push edildiğinden emin olun:

```bash
gcloud container images list --repository=gcr.io/YOUR_PROJECT_ID
```

### "Connection timeout" Hatası

Database connection string'in doğru olduğundan ve Supabase'in Cloud Run'dan erişilebilir olduğundan emin olun.

### Logs Görüntüleme

```bash
# Cloud Run logs
gcloud run services logs read mentorx-api \
  --platform managed \
  --region us-central1 \
  --limit=50
```

## Maliyet

Cloud Run pay-as-you-go model kullanır:
- **CPU**: Sadece request geldiğinde ücretlendirilir
- **Memory**: Kullanılan memory kadar ücretlendirilir
- **Requests**: Her 1M request için ücretlendirilir
- **Free Tier**: İlk 2M request ve 360,000 GB-second ücretsiz

## Güvenlik

- Service account key'leri GitHub Secrets'ta saklanmalı
- Production'da `allow-unauthenticated` yerine IAM authentication kullanılabilir
- Environment variable'lar Cloud Run'da şifrelenmiş olarak saklanır

## Sonraki Adımlar

1. Custom domain ekleme
2. SSL certificate yapılandırma
3. Monitoring ve alerting kurulumu
4. Auto-scaling ayarları optimizasyonu
