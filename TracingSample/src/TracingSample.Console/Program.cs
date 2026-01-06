using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TracingSample.Core.Models;
using TracingSample.Core.Services;
using TracingSample.Tracing.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// ActivitySourceを先に作成して登録
var activitySource = new ActivitySource("TracingSample.Core");
builder.Services.AddSingleton(activitySource);

// OpenTelemetry設定
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "TracingSample",
        serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("TracingSample.Core") // ActivitySourceの名前
            .SetSampler(new AlwaysOnSampler())
            .AddOtlpExporter(options =>
            {
                // JaegerのOTLPエンドポイント
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    });

// サービス登録（トレーシング有効）
builder.Services.AddTracedScoped<IInventoryService, InventoryService>();
builder.Services.AddTracedScoped<IPaymentService, PaymentService>();
builder.Services.AddTracedScoped<IShippingService, ShippingService>();
builder.Services.AddTracedScoped<IOrderService, OrderService>();

// ログ設定
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

// アプリケーション実行
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

    logger.LogInformation("=== OpenTelemetry トレーシングサンプル開始 ===");
    logger.LogInformation("Jaeger UI: http://localhost:16686");
    logger.LogInformation("");

    // サンプル注文を複数実行
    var sampleOrders = new[]
    {
        new
        {
            CustomerId = "CUST-001",
            CustomerName = "田中太郎",
            Address = "東京都渋谷区1-2-3",
            Items = new List<OrderItem>
            {
                new() { ProductId = "PROD-001", ProductName = "ノートPC", Quantity = 1, UnitPrice = 150000 },
                new() { ProductId = "PROD-002", ProductName = "マウス", Quantity = 2, UnitPrice = 3000 }
            }
        },
        new
        {
            CustomerId = "CUST-002",
            CustomerName = "佐藤花子",
            Address = "大阪府大阪市北区4-5-6",
            Items = new List<OrderItem>
            {
                new() { ProductId = "PROD-003", ProductName = "キーボード", Quantity = 1, UnitPrice = 12000 },
                new() { ProductId = "PROD-004", ProductName = "モニター", Quantity = 2, UnitPrice = 35000 }
            }
        },
        new
        {
            CustomerId = "CUST-003",
            CustomerName = "鈴木一郎",
            Address = "愛知県名古屋市中区7-8-9",
            Items = new List<OrderItem>
            {
                new() { ProductId = "PROD-005", ProductName = "スマートフォン", Quantity = 1, UnitPrice = 80000 },
                new() { ProductId = "PROD-006", ProductName = "ケース", Quantity = 1, UnitPrice = 2000 },
                new() { ProductId = "PROD-007", ProductName = "保護フィルム", Quantity = 2, UnitPrice = 1500 }
            }
        }
    };

    for (int i = 0; i < sampleOrders.Length; i++)
    {
        var sample = sampleOrders[i];
        logger.LogInformation("--- 注文 {OrderNumber} 処理開始 ---", i + 1);
        logger.LogInformation("顧客: {CustomerName} ({CustomerId})", sample.CustomerName, sample.CustomerId);
        logger.LogInformation("配送先: {Address}", sample.Address);
        logger.LogInformation("商品数: {ItemCount}", sample.Items.Count);
        logger.LogInformation("");

        try
        {
            var order = await orderService.ProcessOrder(
                sample.CustomerId,
                sample.Items,
                sample.Address
            );

            logger.LogInformation("✓ 注文成功!");
            logger.LogInformation("  注文ID: {OrderId}", order.OrderId);
            logger.LogInformation("  ステータス: {Status}", order.Status);
            logger.LogInformation("  合計金額: ¥{TotalAmount:N0}", order.TotalAmount);
        }
        catch (Exception ex)
        {
            logger.LogError("✗ 注文失敗: {Message}", ex.Message);
        }

        logger.LogInformation("");

        // 次の注文まで少し待機（トレースを見やすくするため）
        if (i < sampleOrders.Length - 1)
        {
            await Task.Delay(1000);
        }
    }

    logger.LogInformation("=== すべての注文処理が完了しました ===");
    logger.LogInformation("Jaeger UIで結果を確認してください: http://localhost:16686");
    logger.LogInformation("");
    logger.LogInformation("※ トレースデータをJaegerに送信しています...");

    // トレースデータを確実にエクスポート
    var provider = scope.ServiceProvider.GetService<TracerProvider>();
    provider?.ForceFlush();
    await Task.Delay(2000);

    logger.LogInformation("トレースデータの送信が完了しました");
}
