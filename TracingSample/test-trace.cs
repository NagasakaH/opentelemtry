using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// テスト用のActivitySource
var activitySource = new ActivitySource("TestTraceService");

// OpenTelemetry設定
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TestTraceService"))
    .AddSource("TestTraceService")
    .AddConsoleExporter()  // コンソールにも出力
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    })
    .Build();

Console.WriteLine("Starting test trace...");

using (var activity = activitySource.StartActivity("TestOperation", ActivityKind.Internal))
{
    activity?.SetTag("test.key", "test.value");
    Console.WriteLine($"Activity created: {activity?.Id ?? "null"}");
    await Task.Delay(100);
}

Console.WriteLine("Trace completed. Flushing...");
tracerProvider?.ForceFlush();
await Task.Delay(2000);
Console.WriteLine("Done. Check Jaeger UI.");
tracerProvider?.Dispose();
