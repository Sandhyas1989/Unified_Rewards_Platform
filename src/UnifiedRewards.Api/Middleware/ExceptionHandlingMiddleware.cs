using System.Net;
using FluentValidation;
using UnifiedRewards.Application.Common.Exceptions;
using UnifiedRewards.Domain.Common;

namespace UnifiedRewards.Api.Middleware;

/// <summary>Translates known application exceptions into clean HTTP responses.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (ValidationException ex)
        {
            await WriteProblem(context, HttpStatusCode.BadRequest, "Validation failed",
                ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (DomainConflictException ex)
        {
            await WriteProblem(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static Task WriteProblem(
        HttpContext context,
        HttpStatusCode status,
        string title,
        IEnumerable<string>? errors = null)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = (int)status,
            title,
            errors = errors?.ToArray()
        });
    }
}
