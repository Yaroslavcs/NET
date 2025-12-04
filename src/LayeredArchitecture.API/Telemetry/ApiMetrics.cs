using System.Diagnostics.Metrics;

namespace LayeredArchitecture.API.Telemetry;

public static class ApiMetrics
{
    public static readonly Meter Meter = new("LayeredArchitecture.API", "1.0.0");
    
    // Cache metrics
    public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("cache_hits_total", "Total number of cache hits");
    public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("cache_misses_total", "Total number of cache misses");
    public static readonly Counter<long> CacheEvictions = Meter.CreateCounter<long>("cache_evictions_total", "Total number of cache evictions");
    public static readonly Histogram<double> CacheHitRatio = Meter.CreateHistogram<double>("cache_hit_ratio", "Cache hit ratio (0-1)");
    
    // gRPC metrics
    public static readonly Counter<long> GrpcRequests = Meter.CreateCounter<long>("grpc_requests_total", "Total number of gRPC requests");
    public static readonly Histogram<double> GrpcRequestDuration = Meter.CreateHistogram<double>("grpc_request_duration_seconds", "gRPC request duration in seconds");
    public static readonly Counter<long> GrpcErrors = Meter.CreateCounter<long>("grpc_errors_total", "Total number of gRPC errors");
    
    // Business metrics
    public static readonly Counter<long> ProductsCreated = Meter.CreateCounter<long>("products_created_total", "Total number of products created");
    public static readonly Counter<long> OrdersProcessed = Meter.CreateCounter<long>("orders_processed_total", "Total number of orders processed");
    public static readonly Counter<long> CustomersRegistered = Meter.CreateCounter<long>("customers_registered_total", "Total number of customers registered");
}