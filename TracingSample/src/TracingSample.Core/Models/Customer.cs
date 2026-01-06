namespace TracingSample.Core.Models;

/// <summary>
/// 顧客情報
/// </summary>
public class Customer
{
    public string CustomerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
}
