/**
 * @file AuditInterceptor.cs
 * @brief Writes an AuditLog row for every change to an audited entity.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Auditing;

/// <summary>
/// Records who created, updated or deleted which record and when, in the AuditLog table.
/// </summary>
/// <remarks>
/// New rows only get their database id during the save, so the interceptor notes the changed
/// entities before the save and writes the audit rows straight after it. If the caller has not
/// opened a transaction, the interceptor opens one around both saves, so a change and its audit
/// rows are committed (or rolled back) together. One interceptor instance serves one DbContext
/// (both are scoped to the request).
/// </remarks>
public class AuditInterceptor : SaveChangesInterceptor
{
    /// <summary>Entity types that are audited, and how each one's public id is read.</summary>
    private static readonly Dictionary<Type, Func<object, string>> AuditedTypes = new()
    {
        [typeof(Staff)] = e => ((Staff)e).StaffNumber,
        [typeof(StaffAssignment)] = e => ((StaffAssignment)e).AssignmentId.ToString(),
        [typeof(RosterEntry)] = e => ((RosterEntry)e).RosterEntryId.ToString(),
        [typeof(Requisition)] = e => ((Requisition)e).RequisitionNumber,
        [typeof(Credential)] = e => ((Credential)e).CredentialId.ToString(),
        [typeof(SecurityFlag)] = e => ((SecurityFlag)e).FlagId.ToString(),
        [typeof(Compensation)] = e => ((Compensation)e).CompensationId.ToString(),
    };

    private readonly ICurrentUser _currentUser;
    private readonly Clock _clock;
    private List<(EntityEntry Entry, string Action)> _pending = [];
    private IDbContextTransaction? _ownTransaction;
    private bool _writingAudit;

    /// <summary>Creates the interceptor.</summary>
    /// <param name="currentUser">Who is making the change.</param>
    /// <param name="clock">Current UTC time.</param>
    public AuditInterceptor(ICurrentUser currentUser, Clock clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    /// <summary>Notes the audited changes and, if needed, opens a transaction.</summary>
    /// <param name="eventData">The save that is starting.</param>
    /// <param name="result">Result so far.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null || _writingAudit) return result;

        _pending = context.ChangeTracker.Entries()
            .Where(e => AuditedTypes.ContainsKey(e.Entity.GetType()))
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => (e, e.State switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                _ => "Updated",
            }))
            .ToList();

        if (_pending.Count > 0 && context.Database.CurrentTransaction == null)
            _ownTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        return result;
    }

    /// <summary>Writes the audit rows now that every changed row has its id.</summary>
    /// <param name="eventData">The save that finished.</param>
    /// <param name="result">Rows affected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null || _writingAudit || _pending.Count == 0) return result;

        var actor = _currentUser.ActorId;
        var now = _clock.UtcNow;
        foreach (var (entry, action) in _pending)
        {
            var entity = entry.Entity;
            context.Add(new AuditLog
            {
                EntityType = entity.GetType().Name,
                EntityId = AuditedTypes[entity.GetType()](entity),
                Action = action,
                ActorId = actor.Length > 100 ? actor[..100] : actor,
                TimestampUtc = now,
            });
        }
        _pending = [];

        try
        {
            _writingAudit = true;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch when (_ownTransaction != null)
        {
            // The audit rows could not be written: undo the change too, so nothing is unaudited.
            await _ownTransaction.RollbackAsync(cancellationToken);
            await _ownTransaction.DisposeAsync();
            _ownTransaction = null;
            throw;
        }
        finally
        {
            _writingAudit = false;
        }

        if (_ownTransaction != null)
        {
            await _ownTransaction.CommitAsync(cancellationToken);
            await _ownTransaction.DisposeAsync();
            _ownTransaction = null;
        }
        return result;
    }

    /// <summary>Rolls back the transaction this interceptor opened, if the save failed.</summary>
    /// <param name="eventData">The failed save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pending = [];
        if (_ownTransaction != null && !_writingAudit)
        {
            await _ownTransaction.RollbackAsync(cancellationToken);
            await _ownTransaction.DisposeAsync();
            _ownTransaction = null;
        }
    }

    /// <summary>Synchronous saves are not used by this application; they are refused so none slips past the audit.</summary>
    /// <param name="eventData">The save that is starting.</param>
    /// <param name="result">Result so far.</param>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        throw new NotSupportedException("Use SaveChangesAsync so the change is audited.");
}
