namespace ProductCatalog.Application.Common.Exceptions;

/// <summary>
/// La versión enviada por el cliente (<c>If-Match</c>) ya no coincide con la almacenada.
/// </summary>
public sealed class PreconditionFailedException : Exception
{
    public PreconditionFailedException()
        : base("El recurso fue modificado por otra solicitud. Vuelve a consultarlo e inténtalo de nuevo.")
    {
    }

    public PreconditionFailedException(string message)
        : base(message)
    {
    }

    public PreconditionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
