using TracingSample.Tracing.Attributes;

namespace TracingSample.Core.Services;

/// <summary>
/// 決済サービスの実装
/// </summary>
public class PaymentService : IPaymentService
{
    [Trace]
    public async Task<string> ProcessPayment(string customerId, decimal amount)
    {
        // 決済処理をシミュレート
        await Task.Delay(200);

        // 模擬的に10%の確率で決済失敗とする
        var random = new Random();
        if (random.Next(100) < 10)
        {
            throw new InvalidOperationException("決済処理に失敗しました。カード情報を確認してください。");
        }

        // トランザクションIDを生成
        var transactionId = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        return transactionId;
    }

    [Trace]
    public async Task RefundPayment(string transactionId)
    {
        // 返金処理をシミュレート
        await Task.Delay(150);
    }
}
