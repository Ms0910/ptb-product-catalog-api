namespace ProductCatalog.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Persiste los cambios rastreados como una única operación atómica.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ejecuta <paramref name="operation"/> dentro de una transacción de base de datos. La
    /// transacción se confirma cuando la operación termina y se revierte si lanza una excepción.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
