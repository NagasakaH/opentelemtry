using TracingSample.Core.Models;

namespace TracingSample.Core.Services;

/// <summary>
/// 注文処理サービス
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 注文を処理します
    /// </summary>
    Task<Order> ProcessOrder(string customerId, List<OrderItem> items, string shippingAddress);
}
