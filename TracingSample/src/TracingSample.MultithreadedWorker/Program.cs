using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TracingSample.Core.Models;
using TracingSample.Core.Services;
using TracingSample.Tracing.Extensions;

// ===== 設定 =====
const int WorkerCount = 3; // ワーカースレッド数
const string ServiceName = "TracingSample.MultithreadedWorker";
const string ServiceVersion = "1.0.0";

var builder = Host.CreateApplicationBuilder(args);

// ActivitySourceを先に作成して登録
var activitySource = new ActivitySource("TracingSample.Core");
builder.Services.AddSingleton(activitySource);

// OpenTelemetryリソース定義（Tracing/Loggingで共通）
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: ServiceName, serviceVersion: ServiceVersion);

// OpenTelemetry Tracing設定
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: ServiceName,
        serviceVersion: ServiceVersion))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("TracingSample.Core")
            .AddSource("TracingSample.MultithreadedWorker")
            .SetSampler(new AlwaysOnSampler())
            .AddOtlpExporter(options =>
            {
                // JaegerのOTLPエンドポイント
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    });

// OpenTelemetry Logging設定（ILoggerの出力をTraceに紐付け）
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    });
});
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// サービス登録（トレーシング有効）
builder.Services.AddTracedScoped<IInventoryService, InventoryService>();
builder.Services.AddTracedScoped<IPaymentService, PaymentService>();
builder.Services.AddTracedScoped<IShippingService, ShippingService>();
builder.Services.AddTracedScoped<IOrderService, OrderService>();

// ワーカー用のActivitySourceを追加
builder.Services.AddSingleton(new ActivitySource("TracingSample.MultithreadedWorker"));

var host = builder.Build();

// CancellationTokenSourceでCtrl-C対応
using var cts = new CancellationTokenSource();

// Console.CancelKeyPressでCtrl-C検出
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; // プロセス終了を防ぐ
    cts.Cancel();
};

// AppDomain.CurrentDomain.ProcessExitでもシャットダウン対応
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    cts.Cancel();
};

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("=== OpenTelemetry マルチスレッドワーカー開始 ===");
logger.LogInformation("サービス名: {ServiceName}", ServiceName);
logger.LogInformation("ワーカー数: {WorkerCount}", WorkerCount);
logger.LogInformation("Jaeger UI: http://localhost:16686");
logger.LogInformation("Ctrl-Cで停止します...");
logger.LogInformation("");

// ホストを開始（OpenTelemetryが初期化される）
await host.StartAsync();

// ワーカータスクを並列で起動
var workerTasks = new List<Task>();
var workerActivitySource = new ActivitySource("TracingSample.MultithreadedWorker");

for (int workerId = 1; workerId <= WorkerCount; workerId++)
{
    var id = workerId; // ラムダキャプチャ用
    var workerTask = Task.Run(async () =>
    {
        await RunWorkerAsync(host.Services, id, workerActivitySource, cts.Token);
    });
    workerTasks.Add(workerTask);
}

try
{
    // すべてのワーカーが終了するまで待機
    await Task.WhenAll(workerTasks);
}
catch (OperationCanceledException)
{
    // キャンセルは正常終了
}

logger.LogInformation("");
logger.LogInformation("=== すべてのワーカーが停止しました ===");

// トレースデータを確実にエクスポート
logger.LogInformation("トレースデータをJaegerに送信しています...");
var provider = host.Services.GetService<TracerProvider>();
provider?.ForceFlush();
await Task.Delay(2000);

logger.LogInformation("グレースフルシャットダウン完了");

// ホストを停止
await host.StopAsync();

/// <summary>
/// ワーカー処理を実行します
/// </summary>
static async Task RunWorkerAsync(
    IServiceProvider services,
    int workerId,
    ActivitySource activitySource,
    CancellationToken cancellationToken)
{
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger($"Worker-{workerId}");
    var random = new Random(workerId * 1000);
    int orderCount = 0;

    logger.LogInformation("Worker-{WorkerId}: 開始", workerId);

    while (!cancellationToken.IsCancellationRequested)
    {
        orderCount++;

        // ワーカー全体のスパンを開始
        using var workerActivity = activitySource.StartActivity($"Worker-{workerId}.ProcessOrder");
        workerActivity?.SetTag("worker.id", workerId);
        workerActivity?.SetTag("worker.order_number", orderCount);

        try
        {
            using var scope = services.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            // サンプル注文データを生成
            var (customerId, customerName, address, items) = GenerateSampleOrder(workerId, orderCount, random);

            logger.LogInformation(
                "Worker-{WorkerId}: 注文 #{OrderNumber} 処理開始 - 顧客: {CustomerName}",
                workerId, orderCount, customerName);

            workerActivity?.SetTag("customer.id", customerId);
            workerActivity?.SetTag("customer.name", customerName);
            workerActivity?.SetTag("order.item_count", items.Count);

            // 注文処理を実行
            var order = await orderService.ProcessOrder(customerId, items, address);

            logger.LogInformation(
                "Worker-{WorkerId}: 注文 #{OrderNumber} 完了 - ID: {OrderId}, 金額: ¥{Amount:N0}",
                workerId, orderCount, order.OrderId, order.TotalAmount);

            workerActivity?.SetTag("order.id", order.OrderId);
            workerActivity?.SetTag("order.status", order.Status.ToString());
            workerActivity?.SetTag("order.total_amount", order.TotalAmount);
            workerActivity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker-{WorkerId}: キャンセルされました", workerId);
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Worker-{WorkerId}: 注文 #{OrderNumber} 処理失敗", workerId, orderCount);
            workerActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            workerActivity?.AddException(ex);
        }

        // 次の処理まで少し待機（ランダムな間隔）
        var delayMs = random.Next(500, 2000);
        try
        {
            await Task.Delay(delayMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }

    logger.LogInformation("Worker-{WorkerId}: 終了（処理した注文数: {OrderCount}）", workerId, orderCount);
}

/// <summary>
/// サンプル注文データを生成します
/// </summary>
static (string CustomerId, string CustomerName, string Address, List<OrderItem> Items) GenerateSampleOrder(
    int workerId, int orderNumber, Random random)
{
    var customers = new[]
    {
        ("CUST-001", "田中太郎", "東京都渋谷区1-2-3"),
        ("CUST-002", "佐藤花子", "大阪府大阪市北区4-5-6"),
        ("CUST-003", "鈴木一郎", "愛知県名古屋市中区7-8-9"),
        ("CUST-004", "高橋美咲", "福岡県福岡市博多区10-11-12"),
        ("CUST-005", "山田健太", "北海道札幌市中央区13-14-15"),
    };

    var products = new[]
    {
        ("PROD-001", "ノートPC", 150000m),
        ("PROD-002", "マウス", 3000m),
        ("PROD-003", "キーボード", 12000m),
        ("PROD-004", "モニター", 35000m),
        ("PROD-005", "スマートフォン", 80000m),
        ("PROD-006", "ケース", 2000m),
        ("PROD-007", "保護フィルム", 1500m),
        ("PROD-008", "USBケーブル", 800m),
        ("PROD-009", "イヤホン", 5000m),
        ("PROD-010", "充電器", 2500m),
    };

    // 顧客をランダムに選択
    var customerIndex = random.Next(customers.Length);
    var (customerId, customerName, address) = customers[customerIndex];

    // ワーカーIDと注文番号で一意になるように顧客IDを調整
    customerId = $"{customerId}-W{workerId}-{orderNumber}";

    // 1〜3個の商品をランダムに選択
    var itemCount = random.Next(1, 4);
    var items = new List<OrderItem>();
    var usedProducts = new HashSet<int>();

    for (int i = 0; i < itemCount; i++)
    {
        int productIndex;
        do
        {
            productIndex = random.Next(products.Length);
        } while (usedProducts.Contains(productIndex));

        usedProducts.Add(productIndex);
        var (productId, productName, unitPrice) = products[productIndex];

        items.Add(new OrderItem
        {
            ProductId = productId,
            ProductName = productName,
            Quantity = random.Next(1, 3),
            UnitPrice = unitPrice
        });
    }

    return (customerId, customerName, address, items);
}
