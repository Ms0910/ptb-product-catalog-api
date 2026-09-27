namespace ProductCatalog.Domain.Common;

/// <summary>
/// Se lanza cuando una operación rompería un invariante de negocio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainException()
        : this("domain.error", "A business rule was violated.")
    {
    }

    public DomainException(string message)
        : this("domain.error", message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "domain.error";
    }

    /// <summary>Código de error estable y legible por máquina (p. ej. <c>product.insufficient_stock</c>).</summary>
    public string Code { get; }
}
