using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Api.ErrorHandling;

/// <summary>
/// Único lugar donde las excepciones se convierten en respuestas HTTP. Cada error se devuelve
/// como un documento problem details RFC 9457, con un <c>code</c> estable y el <c>traceId</c> de la solicitud.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(logger, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var problem = Map(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandledException(logger, problem.Status ?? 0, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        NotFoundException e => Problem(StatusCodes.Status404NotFound, "Resource not found", e.Message, "not_found"),
        InsufficientStockException e => WithExtensions(
            Problem(StatusCodes.Status409Conflict, "Insufficient stock", e.Message, e.Code),
            ("availableStock", e.AvailableStock),
            ("requestedQuantity", e.RequestedQuantity)),
        DomainException e => Problem(StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Message, e.Code),
        ConflictException e => Problem(StatusCodes.Status409Conflict, "Conflict", e.Message, e.Code),
        PreconditionFailedException e => Problem(StatusCodes.Status412PreconditionFailed, "Precondition failed", e.Message, "precondition_failed"),
        UnprocessableRequestException e => Problem(StatusCodes.Status422UnprocessableEntity, "Unprocessable request", e.Message, e.Code),
        BadHttpRequestException e => Problem(e.StatusCode, "Bad request", e.Message, "bad_request"),
        _ => Problem(StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.", "internal_error"),
    };

    private static ProblemDetails Problem(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static ProblemDetails WithExtensions(ProblemDetails problem, params (string Key, object Value)[] extensions)
    {
        foreach (var (key, value) in extensions)
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request rejected with {StatusCode}: {Reason}")]
    private static partial void LogHandledException(ILogger logger, int statusCode, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request to {Path} was aborted by the client")]
    private static partial void LogRequestAborted(ILogger logger, PathString path);
}
