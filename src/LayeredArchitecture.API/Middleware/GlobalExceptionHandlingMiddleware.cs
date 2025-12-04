using System.Net;
using LayeredArchitecture.BLL.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LayeredArchitecture.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = GetStatusCode(exception),
            Title = "An error occurred",
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
        }

        context.Response.StatusCode = problemDetails.Status.Value;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status422UnprocessableEntity,
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            BusinessException ex when ex.Message.Contains("already exists") => StatusCodes.Status409Conflict,
            BusinessException ex when ex.Message.Contains("Insufficient stock") => StatusCodes.Status422UnprocessableEntity,
            BusinessException ex when ex.Message.Contains("price mismatch") => StatusCodes.Status422UnprocessableEntity,
            BusinessException ex when ex.Message.Contains("Only pending orders") => StatusCodes.Status409Conflict,
            BusinessException ex when ex.Message.Contains("existing stock") => StatusCodes.Status409Conflict,
            BusinessException ex when ex.Message.Contains("Invalid status transition") => StatusCodes.Status422UnprocessableEntity,
            BusinessException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}