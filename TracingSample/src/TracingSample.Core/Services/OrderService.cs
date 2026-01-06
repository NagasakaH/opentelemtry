using TracingSample.Core.Models;
using TracingSample.Tracing.Attributes;

namespace TracingSample.Core.Services;

/// <summary>
/// 注文処理サービスの実装
/// </summary>
public class OrderService : IOrderService
{
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;

    public OrderService(
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IShippingService shippingService)
    {
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _shippingService = shippingService;
    }

    [Trace]
    public async Task<Order> ProcessOrder(string customerId, List<OrderItem> items, string shippingAddress)
    {
        var order = new Order
        {
            OrderId = $"ORD-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            CustomerId = customerId,
            Items = items,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        string? transactionId = null;
        string? shipmentId = null;

        try
        {
            // ステップ1: 在庫確認と予約
            await _inventoryService.CheckAndReserveStock(items);
            order.Status = OrderStatus.PaymentProcessing;

            // ステップ2: 決済処理
            transactionId = await _paymentService.ProcessPayment(customerId, order.TotalAmount);
            order.Status = OrderStatus.Paid;

            // ステップ3: 配送手配
            shipmentId = await _shippingService.CreateShipment(order, shippingAddress);
            order.Status = OrderStatus.Shipping;

            // 完了
            order.Status = OrderStatus.Completed;

            return order;
        }
        catch (Exception ex)
        {
            // ロールバック処理
            order.Status = OrderStatus.Failed;

            // 配送がすでに手配されていればキャンセル
            if (shipmentId != null)
            {
                await _shippingService.CancelShipment(shipmentId);
            }

            // 決済が完了していれば返金
            if (transactionId != null)
            {
                await _paymentService.RefundPayment(transactionId);
            }

            // 在庫予約を解除
            await _inventoryService.ReleaseStock(items);

            throw new InvalidOperationException($"注文処理に失敗しました: {ex.Message}", ex);
        }
    }
}
