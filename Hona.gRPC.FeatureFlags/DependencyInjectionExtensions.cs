using Grpc.AspNetCore.Server;

namespace Hona.gRPC.FeatureFlags;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds the <see cref="GrpcFeatureFlagInterceptor"/> to the gRPC service options
    /// </summary>
    public static void UseFeatureFlags(this GrpcServiceOptions options)
    {
        options.Interceptors.Add<GrpcFeatureFlagInterceptor>();
    }
}