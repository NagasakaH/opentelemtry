# OpenTelemetry トレーシングサンプル (.NET 8)

本番環境でメソッドの実行状況やパラメータを追跡できる、OpenTelemetryを使った実践的なサンプルプロジェクトです。

## 特徴

- **既存コードへの影響を最小限に**: メソッドに`[Trace]`アトリビュートを付けるだけで自動トレース
- **.NET標準機能のみ使用**: DispatchProxyを使用し、追加の外部ライブラリ不要
- **詳細な情報記録**: パラメータ、戻り値、例外、実行時間をすべて記録
- **Jaegerで可視化**: Docker一発起動で、ブラウザからトレース確認

## プロジェクト構成

```
TracingSample/
├── src/
│   ├── TracingSample.Console/          # メインアプリケーション
│   ├── TracingSample.Core/             # ビジネスロジック（EC注文処理）
│   └── TracingSample.Tracing/          # トレーシング機能（再利用可能）
├── docker/
│   └── docker-compose.yml              # Jaeger起動設定
└── README.md
```

## 必要な環境

- .NET 8 SDK
- Docker（Jaeger実行用）

## クイックスタート

### 1. Jaegerを起動

```bash
cd docker
docker-compose up -d
```

Jaeger UIが起動します: http://localhost:16686

### 2. アプリケーションを実行

```bash
cd src/TracingSample.Console
dotnet run
```

### 3. Jaeger UIで確認

ブラウザで http://localhost:16686 を開き、以下を確認：

1. **Service** ドロップダウンから `TracingSample` を選択
2. **Find Traces** をクリック
3. トレース一覧から興味のあるトレースをクリック

#### 確認できる情報

- 各メソッドの実行時間（タイムライン表示）
- メソッドの親子関係（呼び出し階層）
- パラメータの値（JSON形式）
- 戻り値（JSON形式）
- 例外情報（スタックトレース含む）

## 使い方

### 基本的な使い方

#### 1. サービスインターフェースを定義

```csharp
public interface IOrderService
{
    [Trace]  // このアトリビュートを追加するだけ！
    Task<Order> ProcessOrder(string customerId, List<OrderItem> items, string address);
}
```

#### 2. サービスを実装

```csharp
public class OrderService : IOrderService
{
    public async Task<Order> ProcessOrder(string customerId, List<OrderItem> items, string address)
    {
        // 通常のビジネスロジック
        // トレーシングコードは不要！
    }
}
```

#### 3. DIコンテナに登録

```csharp
// 通常の登録の代わりに
services.AddTracedScoped<IOrderService, OrderService>();
```

これだけで、メソッド呼び出し時に自動的に以下が記録されます：

- メソッド名、クラス名
- すべてのパラメータ（`customerId`, `items`, `address`）
- 戻り値（`Order`オブジェクト）
- 実行時間
- 例外（発生した場合）

### カスタマイズ

#### トレース名を変更

```csharp
[Trace(Name = "注文処理")]
public async Task<Order> ProcessOrder(...)
```

#### パラメータや戻り値の記録を無効化

```csharp
[Trace(RecordParameters = false, RecordReturnValue = false)]
public async Task<Order> ProcessOrder(...)
```

#### 例外記録を無効化

```csharp
[Trace(RecordException = false)]
public async Task<Order> ProcessOrder(...)
```

## サンプルシナリオ: EC注文処理

このサンプルでは、ECサイトの注文処理フローを実装しています：

```
OrderService.ProcessOrder
├─→ InventoryService.CheckAndReserveStock  (在庫確認・予約)
├─→ PaymentService.ProcessPayment          (決済処理)
└─→ ShippingService.CreateShipment         (配送手配)
```

エラー発生時は自動的にロールバック処理が実行され、すべてのトレースが記録されます。

## アーキテクチャ

### トレーシングの仕組み

1. **DIコンテナ登録時**: `AddTracedScoped`メソッドが、サービスをDispatchProxyでラップ
2. **メソッド呼び出し時**: プロキシがメソッドをインターセプト
3. **[Trace]アトリビュート検出**: アトリビュートがあれば、OpenTelemetry Activityを作成
4. **情報記録**: パラメータ、戻り値、例外などを記録
5. **OTLPエクスポート**: JaegerにgRPCで送信

### DispatchProxy vs Castle.DynamicProxy

このサンプルでは.NET標準の`DispatchProxy`を使用していますが、以下の違いがあります：

| 機能 | DispatchProxy | Castle.DynamicProxy |
|------|---------------|---------------------|
| 外部依存 | なし | NuGetパッケージ必要 |
| インターフェース | ✅ サポート | ✅ サポート |
| クラスベース | ❌ 未サポート | ✅ サポート |
| パフォーマンス | 良好 | より高速 |

本番環境でクラスベースのプロキシが必要な場合は、Castle.DynamicProxyへの切り替えを検討してください。

## トラブルシューティング

### Jaegerにトレースが表示されない

1. Jaegerが起動しているか確認
   ```bash
   docker ps | grep jaeger
   ```

2. ポート4317が開いているか確認
   ```bash
   netstat -an | grep 4317
   ```

3. アプリケーションのログでエラーを確認

### パフォーマンスへの影響

- リフレクション使用によるオーバーヘッド: 1メソッド呼び出しあたり約0.1-0.5ms
- JSON シリアライズのオーバーヘッド: パラメータのサイズに依存
- 本番環境では、重要なメソッドのみに`[Trace]`を付けることを推奨

### 機密情報の除外

パスワードやトークンなどの機密情報を含むパラメータは、以下のいずれかで対応：

1. `RecordParameters = false` で無効化
2. カスタムシリアライザを実装してマスク処理
3. パラメータオブジェクトに`[JsonIgnore]`を使用

## 他のプロジェクトへの導入

### 1. TracingSample.Tracing プロジェクトをコピー

```bash
cp -r src/TracingSample.Tracing /path/to/your/project/
```

### 2. プロジェクト参照を追加

```bash
dotnet add reference ../TracingSample.Tracing/TracingSample.Tracing.csproj
```

### 3. OpenTelemetryを設定

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("YourServiceName")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

### 4. サービス登録を変更

```csharp
// Before
services.AddScoped<IYourService, YourService>();

// After
services.AddTracedScoped<IYourService, YourService>();
```

### 5. トレースしたいメソッドに[Trace]を追加

```csharp
public interface IYourService
{
    [Trace]
    Task YourMethod(string param1, int param2);
}
```

## 応用例

### メトリクスやログとの統合

OpenTelemetryは、トレース、メトリクス、ログを統合できます：

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(...)
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging(logging => logging
        .AddOtlpExporter());
```

### 本番環境での設定

```csharp
.AddOtlpExporter(options =>
{
    // 本番のコレクターエンドポイント
    options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"]);

    // サンプリング設定（すべてのトレースを送信しない）
    tracing.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10%
});
```

## ライセンス

このサンプルコードは自由に使用・改変できます。

## 参考リンク

- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Jaeger](https://www.jaegertracing.io/)
- [DispatchProxy](https://learn.microsoft.com/dotnet/api/system.reflection.dispatchproxy)
