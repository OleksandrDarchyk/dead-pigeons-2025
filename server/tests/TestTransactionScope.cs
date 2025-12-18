using dataccess;
using Microsoft.EntityFrameworkCore.Storage;

namespace tests;

public sealed class TestTransactionScope(MyDbContext context) : IDisposable
{
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_transaction != null)
        {
            await RollbackAndDisposeAsync(ct);
        }

        _transaction = await context.Database.BeginTransactionAsync(ct);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_transaction is null)
            return;

        try
        {
            _transaction.Rollback();
        }
        catch
        {
            // Ignore rollback errors (transaction may already be aborted).
        }
        finally
        {
            try
            {
                _transaction.Dispose();
            }
            catch
            {
                // Ignore dispose errors (best-effort cleanup).
            }

            _transaction = null;
        }
    }

    private async Task RollbackAndDisposeAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        catch
        {
            // Ignore rollback errors (transaction may already be aborted).
        }
        finally
        {
            try
            {
                await _transaction.DisposeAsync();
            }
            catch
            {
                // Ignore dispose errors (best-effort cleanup).
            }

            _transaction = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TestTransactionScope));
    }
}
