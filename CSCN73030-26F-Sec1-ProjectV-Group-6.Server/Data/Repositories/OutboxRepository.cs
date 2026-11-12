/**
 * @file OutboxRepository.cs
 * @brief Database access for the transactional outbox.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core access to <see cref="OutboxEvent"/> rows.</summary>
public class OutboxRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the repository.</summary>
    /// <param name="db">Database context.</param>
    public OutboxRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>Adds an event to be inserted with the next save, in the same transaction as the business change.</summary>
    /// <param name="outboxEvent">The event.</param>
    public void Add(OutboxEvent outboxEvent) => _db.OutboxEvents.Add(outboxEvent);

    /// <summary>The oldest unpublished events, oldest first.</summary>
    /// <param name="max">Most events to return.</param>
    public Task<List<OutboxEvent>> GetPendingAsync(int max) =>
        _db.OutboxEvents
            .Where(e => e.PublishedAtUtc == null)
            .OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.EventId)
            .Take(max)
            .ToListAsync();

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
