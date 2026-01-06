using TracingSample.Core.Models;
using TracingSample.Tracing.Attributes;

namespace TracingSample.Core.Services;

/// <summary>
/// 在庫管理サービスの実装
/// </summary>
public class InventoryService : IInventoryService
{
    [Trace]
    public async Task<bool> CheckAndReserveStock(List<OrderItem> items)
    {
        // 在庫確認処理をシミュレート
        await Task.Delay(100);

        // 各商品の在庫を確認
        foreach (var item in items)
        {
            // 模擬的に20%の確率で在庫不足とする
            var random = new Random();
            if (random.Next(100) < 20)
            {
                throw new InvalidOperationException($"商品 {item.ProductName} (ID: {item.ProductId}) の在庫が不足しています。");
            }

            // 在庫予約処理をシミュレート
            await Task.Delay(50);
        }

        return true;
    }

    [Trace]
    public async Task ReleaseStock(List<OrderItem> items)
    {
        // 在庫解放処理をシミュレート
        await Task.Delay(50);
    }
}
