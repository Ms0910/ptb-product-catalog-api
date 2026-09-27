namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>
/// La solicitud está bien formada pero no puede procesarse tal como fue enviada
/// (p. ej. una clave de idempotencia reusada con un payload distinto).
/// </summary>
public sealed class UnprocessableRequestException : Exception
{
    public UnprocessableRequestException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public UnprocessableRequestException()
    {
    }

    public UnprocessableRequestException(string message)
        : base(message)
    {
    }

    public UnprocessableRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string Code { get; } = "unprocessable_request";
}
