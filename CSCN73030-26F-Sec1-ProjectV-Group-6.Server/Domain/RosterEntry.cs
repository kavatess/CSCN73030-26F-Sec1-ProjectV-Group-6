/**
 * @file RosterEntry.cs
 * @brief RosterEntry entity: one shift on the duty roster.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>One shift on the duty roster. Times are UTC; a shift covers [ShiftStart, ShiftEnd).</summary>
/// @par Business rule 6
/// ShiftStart must be before ShiftEnd, and a person's non-cancelled shifts may not overlap.
public class RosterEntry
{
    /// <summary>Database key, also the id used by <c>DELETE /rosters/entries/{id}</c>.</summary>
    public int RosterEntryId { get; set; }

    /// <summary>Rostered staff member's key.</summary>
    public int StaffId { get; set; }

    /// <summary>Rostered staff member.</summary>
    public Staff Staff { get; set; } = null!;

    /// <summary>Service the shift is worked in.</summary>
    public int ServiceId { get; set; }

    /// <summary>Service the shift is worked in.</summary>
    public Service Service { get; set; } = null!;

    /// <summary>Shift start, UTC (inclusive).</summary>
    public DateTime ShiftStart { get; set; }

    /// <summary>Shift end, UTC (exclusive).</summary>
    public DateTime ShiftEnd { get; set; }

    /// <summary>Day, Evening, Night or OnCall.</summary>
    public ShiftType ShiftType { get; set; }

    /// <summary>Scheduled, Confirmed, Completed or Cancelled.</summary>
    public RosterStatus Status { get; set; }
}
