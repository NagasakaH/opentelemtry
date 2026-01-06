using TracingSample.Core.Models;

namespace TracingSample.Core.Services;

/// <summary>
/// 配送サービス
/// </summary>
public interface IShippingService
{
    /// <summary>
    /// 配送手配を作成します
    /// </summary>
    Task<string> CreateShipment(Order order, string shippingAddress);

    /// <summary>
    /// 配送をキャンセルします
    /// </summary>
    Task CancelShipment(string shipmentId);
}
