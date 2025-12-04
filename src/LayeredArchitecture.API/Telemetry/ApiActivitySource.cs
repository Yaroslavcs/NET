using System.Diagnostics;

namespace LayeredArchitecture.API.Telemetry;

public static class ApiActivitySource
{
    public static readonly ActivitySource Source = new("LayeredArchitecture.API", "1.0.0");
}