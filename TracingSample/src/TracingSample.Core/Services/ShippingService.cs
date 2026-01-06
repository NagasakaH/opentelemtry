using TracingSample.Core.Models;
using TracingSample.Tracing.Attributes;

namespace TracingSample.Core.Services;

/// <summary>
/// 配送サービスの実装
/// </summary>
public class ShippingService : IShippingService
{
    [Trace]
    public async Task<string> CreateShipment(Order order, string shippingAddress)
    {
        // 配送手配処理をシミュレート
        await Task.Delay(150);

        // 配送IDを生成
        var shipmentId = $"SHIP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        return shipmentId;
    }

    [Trace]
    public async Task CancelShipment(string shipmentId)
    {
        // 配送キャンセル処理をシミュレート
        await Task.Delay(100);
    }
}
