using TracingSample.Core.Models;

namespace TracingSample.Core.Services;

/// <summary>
/// 在庫管理サービス
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// 在庫を確認し、予約します
    /// </summary>
    Task<bool> CheckAndReserveStock(List<OrderItem> items);

    /// <summary>
    /// 在庫予約を解除します
    /// </summary>
    Task ReleaseStock(List<OrderItem> items);
}
