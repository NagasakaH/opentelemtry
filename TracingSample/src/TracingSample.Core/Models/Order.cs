namespace TracingSample.Core.Models;

/// <summary>
/// 注文情報
/// </summary>
public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
}

/// <summary>
/// 注文ステータス
/// </summary>
public enum OrderStatus
{
    Pending,
    PaymentProcessing,
    Paid,
    Shipping,
    Completed,
    Failed
}
