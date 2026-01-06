using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using TracingSample.Tracing.Attributes;

namespace TracingSample.Tracing.Interceptors;

/// <summary>
/// DispatchProxyを使用してメソッド呼び出しをインターセプトし、OpenTelemetryトレースを自動生成します。
/// </summary>
/// <typeparam name="T">インターフェース型</typeparam>
public class TracingProxy<T> : DispatchProxy where T : class
{
    private T? _target;
    private ActivitySource? _activitySource;

    /// <summary>
    /// プロキシを作成します。
    /// </summary>
    public static T Create(T target, ActivitySource activitySource)
    {
        var proxy = Create<T, TracingProxy<T>>() as TracingProxy<T>;
        if (proxy == null)
        {
            throw new InvalidOperationException("Failed to create proxy instance.");
        }

        proxy._target = target ?? throw new ArgumentNullException(nameof(target));
        proxy._activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        return (T)(object)proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null || _target == null || _activitySource == null)
        {
            throw new InvalidOperationException("Proxy is not properly initialized.");
        }

        // インターフェースのメソッドではなく、実装クラスのメソッドから属性を取得
        var implementationMethod = _target.GetType().GetMethod(
            targetMethod.Name,
            targetMethod.GetParameters().Select(p => p.ParameterType).ToArray());

        var traceAttribute = implementationMethod?.GetCustomAttribute<TraceAttribute>();

        // [Trace]アトリビュートがない場合は、そのまま実行
        if (traceAttribute == null)
        {
            return targetMethod.Invoke(_target, args);
        }

        // Activityの名前を決定
        var activityName = !string.IsNullOrEmpty(traceAttribute.Name)
            ? traceAttribute.Name
            : $"{targetMethod.DeclaringType?.Name}.{targetMethod.Name}";

        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Internal);

        try
        {
            // メソッド情報をタグとして追加
            activity?.SetTag("method.name", targetMethod.Name);
            activity?.SetTag("method.class", targetMethod.DeclaringType?.FullName);

            // パラメータを記録
            if (traceAttribute.RecordParameters && args != null)
            {
                RecordParameters(activity, targetMethod, args);
            }

            // メソッドを実行
            var result = targetMethod.Invoke(_target, args);

            // 非同期メソッドの場合
            if (result is Task task)
            {
                return HandleAsyncMethod(task, result, activity, traceAttribute);
            }

            // 戻り値を記録
            if (traceAttribute.RecordReturnValue && result != null)
            {
                RecordReturnValue(activity, result);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // リフレクション呼び出しの例外をアンラップ
            if (traceAttribute.RecordException)
            {
                RecordException(activity, ex.InnerException);
            }
            activity?.SetStatus(ActivityStatusCode.Error, ex.InnerException.Message);
            throw ex.InnerException;
        }
        catch (Exception ex)
        {
            if (traceAttribute.RecordException)
            {
                RecordException(activity, ex);
            }
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static object HandleAsyncMethod(Task task, object result, Activity? activity, TraceAttribute traceAttribute)
    {
        var resultType = result.GetType();

        // Task<T>の場合
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var continueMethod = typeof(TracingProxy<T>)
                .GetMethod(nameof(ContinueWithResult), BindingFlags.NonPublic | BindingFlags.Static)?
                .MakeGenericMethod(resultType.GetGenericArguments()[0]);

            return continueMethod?.Invoke(null, new object[] { result, activity!, traceAttribute })
                ?? throw new InvalidOperationException("Failed to invoke ContinueWithResult");
        }

        // Task (非ジェネリック)の場合
        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                if (traceAttribute.RecordException)
                {
                    RecordException(activity, t.Exception.GetBaseException());
                }
                activity?.SetStatus(ActivityStatusCode.Error, t.Exception.GetBaseException().Message);
            }
            else if (t.IsCanceled)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Task was canceled");
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
        }, TaskContinuationOptions.ExecuteSynchronously);

        // 元のタスクを返す（Continuationタスクではなく）
        return task;
    }

    private static async Task<TResult> ContinueWithResult<TResult>(Task<TResult> task, Activity activity, TraceAttribute traceAttribute)
    {
        try
        {
            var result = await task;

            if (traceAttribute.RecordReturnValue && result != null)
            {
                RecordReturnValue(activity, result);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (TaskCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Task was canceled");
            throw;
        }
        catch (Exception ex)
        {
            if (traceAttribute.RecordException)
            {
                RecordException(activity, ex);
            }
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static void RecordParameters(Activity? activity, MethodInfo method, object?[] args)
    {
        if (activity == null) return;

        var parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length && i < args.Length; i++)
        {
            var paramName = parameters[i].Name ?? $"param{i}";
            var paramValue = args[i];

            try
            {
                var serialized = SerializeValue(paramValue);
                activity.SetTag($"parameter.{paramName}", serialized);
            }
            catch (Exception ex)
            {
                activity.SetTag($"parameter.{paramName}", $"[Serialization Error: {ex.Message}]");
            }
        }
    }

    private static void RecordReturnValue(Activity? activity, object returnValue)
    {
        if (activity == null) return;

        try
        {
            var serialized = SerializeValue(returnValue);
            activity.SetTag("return.value", serialized);
        }
        catch (Exception ex)
        {
            activity.SetTag("return.value", $"[Serialization Error: {ex.Message}]");
        }
    }

    private static void RecordException(Activity? activity, Exception exception)
    {
        if (activity == null) return;

        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);

        // OpenTelemetryの標準イベント形式でも記録
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        };

        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
    }

    private static string SerializeValue(object? value)
    {
        if (value == null) return "null";

        var type = value.GetType();

        // プリミティブ型や文字列はそのまま返す
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return value.ToString() ?? "null";
        }

        // それ以外はJSONシリアライズ
        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 5 // 循環参照対策
            });
        }
        catch
        {
            return value.ToString() ?? type.Name;
        }
    }
}
