namespace ProductCatalog.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' no encontrado.")
    {
        Resource = resource;
        Key = key;
    }

    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string Resource { get; } = string.Empty;

    public object? Key { get; }
}
