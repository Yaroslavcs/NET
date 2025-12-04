using Grpc.Core;
using Grpc.Core.Interceptors;
using LayeredArchitecture.API.Telemetry;
using System.Diagnostics;
using System.Text.Json;

namespace LayeredArchitecture.API.Services.Interceptors;

public class TelemetryGrpcInterceptor : Interceptor
{
    private readonly ILogger<TelemetryGrpcInterceptor> _logger;

    public TelemetryGrpcInterceptor(ILogger<TelemetryGrpcInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var activity = ApiActivitySource.Source.StartActivity($"gRPC {context.Method}");
        
        try
        {
            // Record gRPC request metrics
            ApiMetrics.GrpcRequests.Add(1, new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await continuation(request, context);
            stopwatch.Stop();
            
            // Record duration
            ApiMetrics.GrpcRequestDuration.Record(stopwatch.Elapsed.TotalSeconds, 
                new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            activity?.SetTag("grpc.method", context.Method);
            activity?.SetTag("grpc.status", "OK");
            activity?.SetTag("grpc.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogInformation("gRPC call {Method} completed successfully in {Duration}ms", 
                context.Method, stopwatch.Elapsed.TotalMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetTag("grpc.status", "Error");
            activity?.SetTag("grpc.error", ex.Message);
            activity?.RecordException(ex);
            
            // Record error metrics
            ApiMetrics.GrpcErrors.Add(1, 
                new KeyValuePair<string, object?>("grpc.method", context.Method),
                new KeyValuePair<string, object?>("grpc.error", ex.Message));
            
            _logger.LogError(ex, "gRPC call {Method} failed with error: {Error}", 
                context.Method, ex.Message);
            
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var activity = ApiActivitySource.Source.StartActivity($"gRPC {context.Method}");
        
        try
        {
            ApiMetrics.GrpcRequests.Add(1, new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await continuation(requestStream, context);
            stopwatch.Stop();
            
            ApiMetrics.GrpcRequestDuration.Record(stopwatch.Elapsed.TotalSeconds, 
                new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            activity?.SetTag("grpc.method", context.Method);
            activity?.SetTag("grpc.status", "OK");
            activity?.SetTag("grpc.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetTag("grpc.status", "Error");
            activity?.SetTag("grpc.error", ex.Message);
            activity?.RecordException(ex);
            
            ApiMetrics.GrpcErrors.Add(1, 
                new KeyValuePair<string, object?>("grpc.method", context.Method),
                new KeyValuePair<string, object?>("grpc.error", ex.Message));
            
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var activity = ApiActivitySource.Source.StartActivity($"gRPC {context.Method}");
        
        try
        {
            ApiMetrics.GrpcRequests.Add(1, new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await continuation(request, responseStream, context);
            stopwatch.Stop();
            
            ApiMetrics.GrpcRequestDuration.Record(stopwatch.Elapsed.TotalSeconds, 
                new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            activity?.SetTag("grpc.method", context.Method);
            activity?.SetTag("grpc.status", "OK");
            activity?.SetTag("grpc.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            activity?.SetTag("grpc.status", "Error");
            activity?.SetTag("grpc.error", ex.Message);
            activity?.RecordException(ex);
            
            ApiMetrics.GrpcErrors.Add(1, 
                new KeyValuePair<string, object?>("grpc.method", context.Method),
                new KeyValuePair<string, object?>("grpc.error", ex.Message));
            
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var activity = ApiActivitySource.Source.StartActivity($"gRPC {context.Method}");
        
        try
        {
            ApiMetrics.GrpcRequests.Add(1, new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await continuation(requestStream, responseStream, context);
            stopwatch.Stop();
            
            ApiMetrics.GrpcRequestDuration.Record(stopwatch.Elapsed.TotalSeconds, 
                new KeyValuePair<string, object?>("grpc.method", context.Method));
            
            activity?.SetTag("grpc.method", context.Method);
            activity?.SetTag("grpc.status", "OK");
            activity?.SetTag("grpc.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            activity?.SetTag("grpc.status", "Error");
            activity?.SetTag("grpc.error", ex.Message);
            activity?.RecordException(ex);
            
            ApiMetrics.GrpcErrors.Add(1, 
                new KeyValuePair<string, object?>("grpc.method", context.Method),
                new KeyValuePair<string, object?>("grpc.error", ex.Message));
            
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }
}