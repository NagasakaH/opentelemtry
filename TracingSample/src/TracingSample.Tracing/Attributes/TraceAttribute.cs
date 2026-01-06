namespace TracingSample.Tracing.Attributes;

/// <summary>
/// メソッドにこのアトリビュートを付与すると、OpenTelemetryによる自動トレースが有効になります。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class TraceAttribute : Attribute
{
    /// <summary>
    /// トレースの名前（指定しない場合はメソッド名が使用されます）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// パラメータを記録するかどうか（デフォルト: true）
    /// </summary>
    public bool RecordParameters { get; set; } = true;

    /// <summary>
    /// 戻り値を記録するかどうか（デフォルト: true）
    /// </summary>
    public bool RecordReturnValue { get; set; } = true;

    /// <summary>
    /// 例外を記録するかどうか（デフォルト: true）
    /// </summary>
    public bool RecordException { get; set; } = true;
}
