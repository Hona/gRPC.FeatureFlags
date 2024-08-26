# gRPC.FeatureFlags
Support for Microsoft.FeatureManagement within ASP.NET Core gRPC applications. Example being, feature flags in Azure App Config, with the same attribute syntax [FeatureGate] used on controllers, but working for gRPC.

[![NuGet Version](https://img.shields.io/nuget/v/Hona.gRPC.FeatureFlags)](https://www.nuget.org/packages/Hona.gRPC.FeatureFlags)

## Pre-requisites

Assuming you are using code first gRPC services for ASP.NET Core

e.g. packages:

- `protobuf-net.Grpc.AspNetCore`
- `Grpc.AspNetCore.Web`

## Usage

Add the package to your project:

```bash
dotnet add package Hona.gRPC.FeatureFlags
```

Modify your dependency injection to include:

```csharp
using Hona.gRPC.FeatureFlags; // 👈 New code
...
services.AddCodeFirstGrpc(config =>
{
    ...
    config.UseFeatureFlags(); // 👈 New code
});
```

Now you can add the attribute in one of 3 places

- On the Request model
- On the Service
- On the method

```csharp
[FeatureGate("Ping")] // 👈 This works
public class PingRequest { }

public class PingResponse { }

[FeatureGate("Ping")] // 👈 This works
public class MyService : IMyService
{
    // 👇 My preferred location, or on the service
    [FeatureGate("Ping")] // 👈 This works
    [Authorize] // This already works, its nice to have both side by side
    public async Task<PingResponse> PingAsync(PingRequest request, CallContext context = default)
    {
        return new PingResponse();
    }
}
```