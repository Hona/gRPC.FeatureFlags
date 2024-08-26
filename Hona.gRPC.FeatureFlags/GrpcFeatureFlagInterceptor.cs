using System.Diagnostics;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Hona.gRPC.FeatureFlags;

using Grpc.Core.Interceptors;

/// <summary>
/// Interceptor to check if feature flags are enabled for a given gRPC method using the Microsoft.FeatureManagement library
/// </summary>
public sealed class GrpcFeatureFlagInterceptor : Interceptor
{
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<GrpcFeatureFlagInterceptor> _logger;

    /// <summary>
    /// Interceptor to check if feature flags are enabled for a given gRPC method using the Microsoft.FeatureManagement library
    /// </summary>
    public GrpcFeatureFlagInterceptor(IFeatureManager featureManager, ILogger<GrpcFeatureFlagInterceptor> logger)
    {
        _featureManager = featureManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        await EnsureFeatureFlagsEnabled<TRequest>(context);
        return await continuation(request, context);
    }

    /// <inheritdoc />
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        await EnsureFeatureFlagsEnabled<TRequest>(context);
        return await continuation(requestStream, context);
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        await EnsureFeatureFlagsEnabled<TRequest>(context);
        await continuation(request, responseStream, context);
    }

    /// <inheritdoc />
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        await EnsureFeatureFlagsEnabled<TRequest>(context);
        await continuation(requestStream, responseStream, context);
    }

    private async Task EnsureFeatureFlagsEnabled<TRequest>(ServerCallContext context)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Feature flag interceptor invoked for {RequestType}", typeof(TRequest).Name);

        if (TryGetFeatureGateName<TRequest>(context, out var featureNames))
        {
            foreach (var featureName in featureNames!)
            {
                if (!await _featureManager.IsEnabledAsync(featureName))
                {
                    sw.Stop();
                    _logger.LogInformation("Feature flag interceptor completed in {ElapsedMilliseconds}ms. ('{DisabledFlag}' is disabled, returning early)", sw.ElapsedMilliseconds, featureName);
                    throw new RpcException(new Status(StatusCode.NotFound, $"Feature {featureName} is disabled"));
                }
            }
        }

        sw.Stop();
        _logger.LogInformation("Feature flag interceptor completed in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
    }

    private static bool TryGetFeatureGateName<TRequest>(ServerCallContext context, out string[]? featureNames)
    {
        var gateAttributes = typeof(TRequest).GetCustomAttributes(typeof(FeatureGateAttribute), true).Cast<FeatureGateAttribute>().ToArray();

        // This Metadata includes the attributes from the method or parent class (i.e. same as you'd expect your authorize attributes to work)
        var methodGateAttributes = context.GetHttpContext().GetEndpoint()?.Metadata.GetOrderedMetadata<FeatureGateAttribute>();

        if (gateAttributes.Length == 0 && methodGateAttributes is null or { Count: 0 })
        {
            featureNames = null;
            return false;
        }

        featureNames = gateAttributes.SelectMany(a => a.Features).Concat(methodGateAttributes?.SelectMany(a => a.Features) ?? []).Distinct().ToArray();
        return true;
    }
}
