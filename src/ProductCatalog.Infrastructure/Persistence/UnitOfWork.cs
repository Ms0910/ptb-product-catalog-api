using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProductCatalog.Application.Abstractions;
using ProductCatalog.Application.Common.Exceptions;

namespace ProductCatalog.Infrastructure.Persistence;

internal sealed class UnitOfWork(CatalogDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(
                "concurrency_conflict",
                "El recurso fue modificado por otra solicitud. Vuelve a consultarlo e inténtalo de nuevo.",
                exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres)
        {
            throw postgres.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => new ConflictException("duplicate", "Ya existe un recurso con el mismo valor único.", exception),
                PostgresErrorCodes.CheckViolation => new ConflictException("constraint_violation", "El cambio incumple una regla de integridad de datos.", exception),
                PostgresErrorCodes.ForeignKeyViolation => new ConflictException("constraint_violation", "El cambio hace referencia a un recurso que ya no existe.", exception),
                _ => exception,
            };
        }
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Con EnableRetryOnFailure, las transacciones de usuario deben ejecutarse a través de la
        // execution strategy para que un fallo transitorio reintente toda la unidad de trabajo.
        var strategy = context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(
            async token =>
            {
                // Un intento reintentado no debe reutilizar entidades rastreadas por el intento fallido.
                context.ChangeTracker.Clear();

                await using var transaction = await context.Database.BeginTransactionAsync(token);

                var result = await operation(token);

                await transaction.CommitAsync(token);

                return result;
            },
            cancellationToken);
    }
}
