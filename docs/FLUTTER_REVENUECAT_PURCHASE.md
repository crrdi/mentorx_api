# Flutter RevenueCat Purchase Implementation Guide

Bu doküman, Flutter uygulamasında RevenueCat ile kredi satın alma işlemini backend'e bildirmek için yapılması gerekenleri detaylandırır.

---

## Genel Bakış

**Flow:**
```
User taps "Buy Credits" 
  → RevenueCat SDK purchase işlemi başlatır
  → Purchase tamamlanır (Apple/Google Store)
  → Flutter transaction bilgilerini alır
  → Backend'e POST /api/credits/purchase-revenuecat isteği gönderir
  → Backend RevenueCat API'den transaction'ı doğrular
  → Kredi eklenir ve response döner
  → Flutter UI'ı günceller
```

---

## 1. RevenueCat SDK Setup

### 1.1 Dependencies

`pubspec.yaml` dosyasına ekleyin:

```yaml
dependencies:
  purchases_flutter: ^7.0.0  # veya en son versiyon
  http: ^1.1.0
```

### 1.2 RevenueCat Initialization

Uygulama başlangıcında (örneğin `main.dart` veya auth service'te):

```dart
import 'package:purchases_flutter/purchases_flutter.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

class RevenueCatService {
  static const String revenueCatApiKey = 'YOUR_REVENUECAT_API_KEY'; // iOS/Android için farklı olabilir
  
  static Future<void> initialize(String? userId) async {
    PurchasesConfiguration configuration;
    
    // Platform'a göre API key seç
    if (Platform.isIOS) {
      configuration = PurchasesConfiguration('YOUR_IOS_API_KEY');
    } else if (Platform.isAndroid) {
      configuration = PurchasesConfiguration('YOUR_ANDROID_API_KEY');
    } else {
      throw UnsupportedError('Platform not supported');
    }
    
    // ÖNEMLİ: Supabase User.Id'yi app_user_id olarak kullan
    // Böylece backend'de kullanıcı eşleşmesi doğrudan yapılır
    await Purchases.configure(configuration);
    
    if (userId != null) {
      await Purchases.logIn(userId); // Supabase User.Id (Guid format)
    }
  }
  
  // User login olduğunda çağrılmalı
  static Future<void> setUserId(String userId) async {
    await Purchases.logIn(userId);
  }
  
  // User logout olduğunda çağrılmalı
  static Future<void> logout() async {
    await Purchases.logOut();
  }
}
```

**Önemli Notlar:**
- `appUserID` olarak **Supabase User.Id** kullanın (Guid format: `550e8400-e29b-41d4-a716-446655440000`)
- Bu sayede backend'de kullanıcı eşleşmesi doğrudan yapılır
- Anonymous ID (`$RCAnonymousID:...`) kullanmayın, backend bunu da destekler ama Guid tercih edilir

---

## 2. Purchase İşlemi

### 2.1 Purchase Function

```dart
import 'package:purchases_flutter/purchases_flutter.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class CreditPurchaseService {
  final String backendBaseUrl = 'https://mentorx-api-gr2ceodgsq-uc.a.run.app';
  final String? supabaseToken; // JWT token for backend authentication
  
  CreditPurchaseService(this.supabaseToken);
  
  /// RevenueCat ile kredi paketi satın alır ve backend'e bildirir
  Future<PurchaseResult> purchaseCredits({
    required Package package,
    required Function(String) onProgress, // Progress callback
  }) async {
    try {
      // 1. RevenueCat purchase işlemini başlat
      onProgress('Starting purchase...');
      
      CustomerInfo customerInfoBefore = await Purchases.getCustomerInfo();
      
      // 2. Purchase işlemini yap
      onProgress('Processing payment...');
      PurchasesPackage purchasesPackage = package as PurchasesPackage;
      CustomerInfo customerInfo = await Purchases.purchasePackage(purchasesPackage);
      
      // 3. Transaction bilgilerini al
      String? transactionId;
      String? productId;
      
      // Transaction ID'yi al (non-subscription purchases için)
      if (customerInfo.nonSubscriptions.isNotEmpty) {
        // En son satın alınan non-subscription purchase'ı bul
        var latestPurchase = customerInfo.nonSubscriptions.values
            .expand((list) => list)
            .where((transaction) => transaction.productIdentifier == package.storeProduct.identifier)
            .toList();
        
        if (latestPurchase.isNotEmpty) {
          // En yeni transaction'ı al
          latestPurchase.sort((a, b) => b.purchaseDate.compareTo(a.purchaseDate));
          var transaction = latestPurchase.first;
          
          transactionId = transaction.transactionIdentifier;
          productId = transaction.productIdentifier;
        }
      }
      
      // Eğer non-subscription'da bulamazsak, active entitlements'tan bak
      if (transactionId == null && customerInfo.entitlements.active.isNotEmpty) {
        var activeEntitlement = customerInfo.entitlements.active.values.first;
        if (activeEntitlement.latestPurchaseDate != null) {
          // Subscription için originalTransactionId kullan
          transactionId = activeEntitlement.originalTransactionId;
          productId = activeEntitlement.productIdentifier;
        }
      }
      
      // Eğer hala transaction ID bulunamazsa, customerInfo'dan al
      if (transactionId == null) {
        // CustomerInfo'dan transaction ID çıkarmaya çalış
        // Bu durumda backend'e sadece productId gönder, backend RevenueCat API'den bulur
        productId = package.storeProduct.identifier;
      }
      
      if (productId == null) {
        throw Exception('Product ID not found');
      }
      
      // 4. Backend'e transaction'ı bildir
      onProgress('Verifying purchase...');
      
      var verifyResult = await verifyPurchaseWithBackend(
        transactionId: transactionId,
        productId: productId,
      );
      
      if (!verifyResult.success) {
        throw Exception(verifyResult.error ?? 'Purchase verification failed');
      }
      
      onProgress('Purchase completed!');
      
      return PurchaseResult(
        success: true,
        creditsAdded: verifyResult.creditsAdded ?? 0,
        newBalance: verifyResult.newBalance ?? 0,
      );
      
    } on PlatformException catch (e) {
      // RevenueCat purchase hatası
      String errorMessage = 'Purchase failed';
      
      if (e.code == PurchasesErrorHelper.purchaseCancelledErrorCode) {
        errorMessage = 'Purchase cancelled';
      } else if (e.code == PurchasesErrorHelper.purchaseNotAllowedErrorCode) {
        errorMessage = 'Purchase not allowed';
      } else if (e.code == PurchasesErrorHelper.purchaseInvalidErrorCode) {
        errorMessage = 'Invalid purchase';
      }
      
      return PurchaseResult(
        success: false,
        error: errorMessage,
      );
    } catch (e) {
      return PurchaseResult(
        success: false,
        error: e.toString(),
      );
    }
  }
  
  /// Backend'e purchase'ı bildirir ve doğrular
  Future<VerifyPurchaseResponse> verifyPurchaseWithBackend({
    String? transactionId,
    required String productId,
  }) async {
    if (supabaseToken == null) {
      throw Exception('User not authenticated');
    }
    
    final url = Uri.parse('$backendBaseUrl/api/credits/purchase-revenuecat');
    
    final requestBody = {
      'transactionId': transactionId ?? '', // Eğer null ise boş string gönder
      'productId': productId,
      // packageId optional, backend productId'den bulur
    };
    
    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $supabaseToken',
      },
      body: jsonEncode(requestBody),
    );
    
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return VerifyPurchaseResponse.fromJson(data);
    } else if (response.statusCode == 400) {
      final data = jsonDecode(response.body) as Map<String, dynamic>;
      return VerifyPurchaseResponse(
        success: false,
        verified: data['verified'] ?? false,
        error: data['error'] ?? 'Bad request',
      );
    } else if (response.statusCode == 401) {
      throw Exception('Unauthorized - Please login again');
    } else {
      throw Exception('Server error: ${response.statusCode}');
    }
  }
}

// Response Models
class PurchaseResult {
  final bool success;
  final int? creditsAdded;
  final int? newBalance;
  final String? error;
  
  PurchaseResult({
    required this.success,
    this.creditsAdded,
    this.newBalance,
    this.error,
  });
}

class VerifyPurchaseResponse {
  final bool success;
  final bool verified;
  final int? creditsAdded;
  final int? newBalance;
  final String? error;
  
  VerifyPurchaseResponse({
    required this.success,
    required this.verified,
    this.creditsAdded,
    this.newBalance,
    this.error,
  });
  
  factory VerifyPurchaseResponse.fromJson(Map<String, dynamic> json) {
    return VerifyPurchaseResponse(
      success: json['success'] ?? false,
      verified: json['verified'] ?? false,
      creditsAdded: json['creditsAdded'],
      newBalance: json['newBalance'],
      error: json['error'],
    );
  }
}
```

---

## 3. UI Implementation

### 3.1 Purchase Button Widget

```dart
import 'package:flutter/material.dart';
import 'package:purchases_flutter/purchases_flutter.dart';

class CreditPurchaseButton extends StatefulWidget {
  final Package package;
  final String? authToken;
  final Function(int newBalance)? onPurchaseSuccess;
  
  const CreditPurchaseButton({
    Key? key,
    required this.package,
    required this.authToken,
    this.onPurchaseSuccess,
  }) : super(key: key);
  
  @override
  State<CreditPurchaseButton> createState() => _CreditPurchaseButtonState();
}

class _CreditPurchaseButtonState extends State<CreditPurchaseButton> {
  bool _isPurchasing = false;
  String? _progressMessage;
  
  Future<void> _handlePurchase() async {
    if (_isPurchasing) return;
    
    setState(() {
      _isPurchasing = true;
      _progressMessage = 'Starting purchase...';
    });
    
    try {
      final service = CreditPurchaseService(widget.authToken);
      
      final result = await service.purchaseCredits(
        package: widget.package,
        onProgress: (message) {
          setState(() {
            _progressMessage = message;
          });
        },
      );
      
      if (result.success) {
        // Başarılı
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'Success! ${result.creditsAdded} credits added. New balance: ${result.newBalance}',
              ),
              backgroundColor: Colors.green,
            ),
          );
          
          // Callback çağır
          if (widget.onPurchaseSuccess != null && result.newBalance != null) {
            widget.onPurchaseSuccess!(result.newBalance!);
          }
        }
      } else {
        // Hata
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(result.error ?? 'Purchase failed'),
              backgroundColor: Colors.red,
            ),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _isPurchasing = false;
          _progressMessage = null;
        });
      }
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (_progressMessage != null)
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: Text(
              _progressMessage!,
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
        ElevatedButton(
          onPressed: _isPurchasing ? null : _handlePurchase,
          child: _isPurchasing
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text('Buy ${widget.package.storeProduct.localizedPriceString}'),
        ),
      ],
    );
  }
}
```

---

## 4. Error Handling & Retry Logic

### 4.1 Retry Mekanizması

Backend'e istek gönderirken retry logic ekleyin:

```dart
Future<VerifyPurchaseResponse> verifyPurchaseWithBackend({
  String? transactionId,
  required String productId,
  int maxRetries = 3,
}) async {
  if (supabaseToken == null) {
    throw Exception('User not authenticated');
  }
  
  final url = Uri.parse('$backendBaseUrl/api/credits/purchase-revenuecat');
  
  final requestBody = {
    'transactionId': transactionId ?? '',
    'productId': productId,
  };
  
  int retryCount = 0;
  Exception? lastException;
  
  while (retryCount < maxRetries) {
    try {
      final response = await http.post(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $supabaseToken',
        },
        body: jsonEncode(requestBody),
      ).timeout(
        const Duration(seconds: 10),
        onTimeout: () {
          throw TimeoutException('Request timeout');
        },
      );
      
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        return VerifyPurchaseResponse.fromJson(data);
      } else if (response.statusCode == 400) {
        // Bad request - don't retry
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        return VerifyPurchaseResponse(
          success: false,
          verified: data['verified'] ?? false,
          error: data['error'] ?? 'Bad request',
        );
      } else if (response.statusCode == 401) {
        // Unauthorized - don't retry
        throw Exception('Unauthorized - Please login again');
      } else if (response.statusCode >= 500) {
        // Server error - retry
        throw Exception('Server error: ${response.statusCode}');
      } else {
        // Other errors - don't retry
        throw Exception('Request failed: ${response.statusCode}');
      }
    } on TimeoutException catch (e) {
      lastException = e;
      retryCount++;
      
      if (retryCount < maxRetries) {
        // Exponential backoff: 1s, 2s, 4s
        await Future.delayed(Duration(seconds: 1 << (retryCount - 1)));
        continue;
      }
    } on SocketException catch (e) {
      // Network error - retry
      lastException = e;
      retryCount++;
      
      if (retryCount < maxRetries) {
        await Future.delayed(Duration(seconds: 1 << (retryCount - 1)));
        continue;
      }
    } catch (e) {
      // Other errors - don't retry
      rethrow;
    }
  }
  
  throw lastException ?? Exception('Max retries exceeded');
}
```

---

## 5. Transaction ID Bulma (Alternatif Yöntem)

Eğer RevenueCat SDK'dan transaction ID'yi bulamazsanız, backend'e sadece `productId` gönderebilirsiniz. Backend RevenueCat API'den customer'ın purchase history'sini çeker ve transaction'ı bulur.

```dart
// Transaction ID bulunamazsa, sadece productId gönder
var verifyResult = await verifyPurchaseWithBackend(
  transactionId: null, // veya boş string
  productId: package.storeProduct.identifier,
);
```

**Not:** Backend'de `transactionId` null veya boş string ise, RevenueCat API'den customer'ın tüm purchase'larını çeker ve en yeni purchase'ı bulur.

---

## 6. Idempotency (Duplicate Prevention)

Backend idempotency kontrolü yapar. Aynı `transactionId` ile tekrar istek gönderilirse:

- **200 OK** döner
- `success: true`, `verified: true`
- `error: "Transaction already processed"`
- `creditsAdded` ve `newBalance` değerleri döner

Bu durumda UI'da kullanıcıya "Credits already added" mesajı gösterilebilir.

---

## 7. Test Senaryoları

### 7.1 Başarılı Purchase

```dart
// 1. User package seçer
// 2. Purchase button'a tıklar
// 3. Apple/Google Store payment flow
// 4. Purchase tamamlanır
// 5. Backend'e istek gönderilir
// 6. Backend doğrular ve kredi ekler
// 7. UI güncellenir
```

### 7.2 Purchase İptal

```dart
// 1. User purchase'ı iptal eder
// 2. PlatformException (purchaseCancelledErrorCode) fırlatılır
// 3. UI'da "Purchase cancelled" mesajı gösterilir
// 4. Backend'e istek gönderilmez
```

### 7.3 Network Hatası

```dart
// 1. Purchase tamamlanır
// 2. Backend'e istek gönderilirken network hatası
// 3. Retry logic devreye girer (3 kez dener)
// 4. Başarısız olursa kullanıcıya bildirilir
// 5. Kullanıcı tekrar deneyebilir (idempotency sayesinde duplicate olmaz)
```

### 7.4 Duplicate Request

```dart
// 1. Purchase tamamlanır
// 2. Backend'e istek gönderilir, kredi eklenir
// 3. Kullanıcı tekrar butona tıklar (örneğin network gecikmesi nedeniyle)
// 4. Backend idempotency kontrolü yapar
// 5. "Transaction already processed" döner
// 6. UI'da "Credits already added" mesajı gösterilir
```

---

## 8. Best Practices

1. **Always use Supabase User.Id as app_user_id:**
   ```dart
   await Purchases.logIn(supabaseUser.id);
   ```

2. **Handle all error cases:**
   - Purchase cancelled
   - Network errors
   - Server errors
   - Invalid purchase

3. **Show progress to user:**
   - "Starting purchase..."
   - "Processing payment..."
   - "Verifying purchase..."
   - "Purchase completed!"

4. **Retry logic:**
   - Network errors için exponential backoff
   - Max 3 retry
   - Server errors (5xx) için retry
   - Client errors (4xx) için retry yapma

5. **Idempotency:**
   - Backend otomatik idempotency kontrolü yapar
   - Aynı transaction ID ile tekrar istek gönderilebilir
   - Duplicate kredi eklenmez

6. **Logging:**
   - Tüm purchase işlemlerini log'la
   - Error'ları log'la
   - Analytics'e gönder (opsiyonel)

---

## 9. Complete Example

```dart
// main.dart veya app initialization
void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Supabase initialize
  await Supabase.initialize(
    url: 'YOUR_SUPABASE_URL',
    anonKey: 'YOUR_SUPABASE_ANON_KEY',
  );
  
  // RevenueCat initialize
  final user = Supabase.instance.client.auth.currentUser;
  await RevenueCatService.initialize(user?.id);
  
  runApp(MyApp());
}

// Purchase screen
class CreditPurchaseScreen extends StatefulWidget {
  @override
  State<CreditPurchaseScreen> createState() => _CreditPurchaseScreenState();
}

class _CreditPurchaseScreenState extends State<CreditPurchaseScreen> {
  List<Package> _packages = [];
  bool _isLoading = true;
  int _currentBalance = 0;
  
  @override
  void initState() {
    super.initState();
    _loadPackages();
    _loadBalance();
  }
  
  Future<void> _loadPackages() async {
    try {
      final offerings = await Purchases.getOfferings();
      if (offerings.current != null) {
        setState(() {
          _packages = offerings.current!.availablePackages;
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _isLoading = false;
      });
    }
  }
  
  Future<void> _loadBalance() async {
    // Backend'den balance çek
    final token = Supabase.instance.client.auth.currentSession?.accessToken;
    if (token == null) return;
    
    final response = await http.get(
      Uri.parse('https://mentorx-api-gr2ceodgsq-uc.a.run.app/api/credits/balance'),
      headers: {'Authorization': 'Bearer $token'},
    );
    
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      setState(() {
        _currentBalance = data['credits'] ?? 0;
      });
    }
  }
  
  Future<void> _handlePurchase(Package package) async {
    final token = Supabase.instance.client.auth.currentSession?.accessToken;
    if (token == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please login first')),
      );
      return;
    }
    
    final service = CreditPurchaseService(token);
    
    final result = await service.purchaseCredits(
      package: package,
      onProgress: (message) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message)),
        );
      },
    );
    
    if (result.success) {
      await _loadBalance(); // Balance'ı yeniden yükle
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Buy Credits')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(
                    'Current Balance: $_currentBalance credits',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                ),
                Expanded(
                  child: ListView.builder(
                    itemCount: _packages.length,
                    itemBuilder: (context, index) {
                      final package = _packages[index];
                      return ListTile(
                        title: Text(package.storeProduct.localizedTitle),
                        subtitle: Text(package.storeProduct.localizedPriceString),
                        trailing: ElevatedButton(
                          onPressed: () => _handlePurchase(package),
                          child: const Text('Buy'),
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
    );
  }
}
```

---

## 10. Troubleshooting

### Problem: Transaction ID bulunamıyor

**Çözüm:** Backend'e `transactionId: null` veya boş string gönderin. Backend RevenueCat API'den customer'ın purchase history'sini çeker ve transaction'ı bulur.

### Problem: "User not found" hatası

**Çözüm:** RevenueCat'te `app_user_id` olarak Supabase User.Id kullanıldığından emin olun:
```dart
await Purchases.logIn(supabaseUser.id);
```

### Problem: "Transaction verification failed"

**Çözüm:** 
- Purchase'ın tamamlandığından emin olun
- Product ID'nin doğru olduğundan emin olun
- RevenueCat API key'in doğru yapılandırıldığından emin olun

### Problem: Network timeout

**Çözüm:** Retry logic kullanın ve timeout süresini artırın (örneğin 30 saniye).

---

**Son Güncelleme:** 2026-02-12
