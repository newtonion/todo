using System;
using System.Net;
using System.Text.Json;
using Api.Domain.Exceptions;

namespace Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;

            _logger.LogError(
                ex,
                "Unhandled exception occurred. TraceId: {TraceId}",
                traceId);

            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string traceId)
    {
        context.Response.ContentType = "application/json";

        var correlationId = context.Items.TryGetValue(CorrelationMiddleware.HeaderName, out var value)
            ? value?.ToString() ?? context.TraceIdentifier
            : context.TraceIdentifier;

        var statusCode = exception switch
        {
            ValidationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            NotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            CorrelationId = correlationId,
            Status = (int)statusCode,
            TraceId = traceId,
            Title = GetTitle(statusCode),
        };

        if (_environment.IsDevelopment())
        {
            response.Debug = exception.ToString();
        }

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }

    private string GetTitle(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Validation error",
            HttpStatusCode.NotFound => "Resource not found",
            HttpStatusCode.Unauthorized => "Unauthorized",
            _ => "An unexpected error occurred"
        };
    }
}

public class ErrorResponse
{
    public int Status { get; set; }

    public string? Debug { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;
}