using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TracingSample.Tracing.Interceptors;

namespace TracingSample.Tracing.Extensions;

/// <summary>
/// IServiceCollectionの拡張メソッドを提供します。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// トレースを有効にしたサービスをScopedライフタイムで登録します。
    /// </summary>
    public static IServiceCollection AddTracedScoped<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return AddTracedService<TInterface, TImplementation>(services, ServiceLifetime.Scoped);
    }

    /// <summary>
    /// トレースを有効にしたサービスをTransientライフタイムで登録します。
    /// </summary>
    public static IServiceCollection AddTracedTransient<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return AddTracedService<TInterface, TImplementation>(services, ServiceLifetime.Transient);
    }

    /// <summary>
    /// トレースを有効にしたサービスをSingletonライフタイムで登録します。
    /// </summary>
    public static IServiceCollection AddTracedSingleton<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return AddTracedService<TInterface, TImplementation>(services, ServiceLifetime.Singleton);
    }

    private static IServiceCollection AddTracedService<TInterface, TImplementation>(
        IServiceCollection services,
        ServiceLifetime lifetime)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        // ActivitySourceは既にProgram.csで登録されているはず
        // （念のため、まだ登録されていない場合は登録する）
        services.TryAddSingleton(sp =>
        {
            var serviceName = "TracingSample.Core";
            return new ActivitySource(serviceName);
        });

        // 実装クラスを登録
        services.Add(new ServiceDescriptor(
            typeof(TImplementation),
            typeof(TImplementation),
            lifetime));

        // プロキシでラップしたインターフェースを登録
        services.Add(new ServiceDescriptor(
            typeof(TInterface),
            sp =>
            {
                var implementation = sp.GetRequiredService<TImplementation>();
                var activitySource = sp.GetRequiredService<ActivitySource>();
                return TracingProxy<TInterface>.Create(implementation, activitySource);
            },
            lifetime));

        return services;
    }

    /// <summary>
    /// OpenTelemetryのActivitySourceの名前を取得します。
    /// </summary>
    public static string GetActivitySourceName<T>()
    {
        return typeof(T).Assembly.GetName().Name ?? "TracingSample";
    }
}
