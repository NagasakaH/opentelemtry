namespace TracingSample.Core.Services;

/// <summary>
/// 決済サービス
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// 決済処理を実行します
    /// </summary>
    Task<string> ProcessPayment(string customerId, decimal amount);

    /// <summary>
    /// 決済を取り消します
    /// </summary>
    Task RefundPayment(string transactionId);
}
