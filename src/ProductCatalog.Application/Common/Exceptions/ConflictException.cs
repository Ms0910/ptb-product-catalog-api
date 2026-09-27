namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>
/// La solicitud no puede completarse debido al estado actual del recurso
/// (p. ej. una modificación concurrente o una restricción de unicidad).
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public ConflictException()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string Code { get; } = "conflict";
}
