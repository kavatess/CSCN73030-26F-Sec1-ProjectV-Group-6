/**
 * @file BudgetFundingService.cs
 * @brief Sends hire requisitions to Budget and Treasury for funding and applies Budget's decisions.
 *
 * HR sends a requisition in HRReview to Budget: Staff/HR creates a funding request in Budget's Finance API
 * and moves the requisition to AwaitingBudget. Budget decides in its own system; Staff/HR reads the decision
 * back (on demand, and from BudgetSyncService every few minutes) and moves the requisition to Approved or
 * Rejected. The manual PATCH /requisitions/{number} stays available as a fallback.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Budget;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Funding requests for hire requisitions, through Budget's Finance API.</summary>
public class BudgetFundingService
{
    /// <summary>Longest reason text sent to Budget.</summary>
    public const int MaxReasonLength = 200;

    private readonly RequisitionRepository _requisitionRepository;
    private readonly CompensationRepository _compensationRepository;
    private readonly BudgetClient _budget;
    private readonly Clock _clock;
    private readonly ILogger<BudgetFundingService> _logger;

    /// <summary>Creates the service.</summary>
    public BudgetFundingService(RequisitionRepository requisitionRepository, CompensationRepository compensationRepository,
        BudgetClient budget, Clock clock, ILogger<BudgetFundingService> logger)
    {
        _requisitionRepository = requisitionRepository;
        _compensationRepository = compensationRepository;
        _budget = budget;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Sends a hire requisition to Budget: creates the funding request, then moves the requisition to
    /// AwaitingBudget with budgetDecision Pending. Nothing is saved if Budget refuses or cannot be reached.
    /// </summary>
    /// <exception cref="NotFoundException">No such requisition (404).</exception>
    /// <exception cref="ConflictException">It was already sent to Budget (409).</exception>
    /// <exception cref="ValidationFailedException">Not a hire in HRReview, or no cost to send (422).</exception>
    /// <exception cref="BudgetServiceException">Budget failed (502).</exception>
    /// <exception cref="BudgetNotConfiguredException">The integration is off (503).</exception>
    public async Task<RequisitionDto> RequestAsync(string requisitionNumber, RequestFundingRequest request)
    {
        var requisition = await _requisitionRepository.GetByNumberAsync(requisitionNumber)
                          ?? throw new NotFoundException("Requisition", requisitionNumber);
        if (requisition.FundingRequestId != null)
            throw new ConflictException(
                $"{requisition.RequisitionNumber} was already sent to Budget (funding request {requisition.FundingRequestId}).");

        var errors = new ValidationErrors();
        errors.AddIf(requisition.Type != RequisitionType.Hire, "type", "Only hire requisitions are funded by Budget.");
        errors.AddIf(requisition.Type == RequisitionType.Hire &&
                     requisition.Status is not (RequisitionStatus.HRReview or RequisitionStatus.AwaitingBudget), "status",
            $"Only a requisition in HRReview can be sent to Budget; this one is {requisition.Status}.");
        errors.AddIf(request.AnnualCostPerPosition is <= 0 or > PayrollService.MaxAnnualSalary, "annualCostPerPosition",
            $"AnnualCostPerPosition must be more than 0 and at most {PayrollService.MaxAnnualSalary:N0}.");
        errors.ThrowIfAny();

        var perPosition = request.AnnualCostPerPosition ?? await EstimateAsync(requisition)
            ?? throw new ValidationFailedException("annualCostPerPosition",
                $"Nobody in {requisition.RequestingDepartment.Code} has a salary on record to estimate from. Send annualCostPerPosition.");
        var amount = Math.Round(perPosition * requisition.Positions, 2);
        var urgent = request.Urgent ?? false;

        var created = await _budget.CreateFundingRequestAsync(
            _budget.ToBudgetCode(requisition.RequestingDepartment.Code), amount, ReasonFor(requisition), urgent);

        var now = _clock.UtcNow;
        requisition.FundingRequestId = created.Id;
        requisition.FundingAmount = amount;
        requisition.FundingUrgent = urgent;
        requisition.FundingRequestedAtUtc = now;
        requisition.FundingCheckedAtUtc = now;
        requisition.Status = RequisitionStatus.AwaitingBudget;
        requisition.BudgetDecision = BudgetDecision.Pending;
        Apply(requisition, created, now);
        await _requisitionRepository.SaveChangesAsync();

        _logger.LogInformation("Sent {Requisition} to Budget as funding request {FundingRequest} for {Amount}",
            requisition.RequisitionNumber, created.Id, amount);
        return requisition.ToDto();
    }

    /// <summary>Asks Budget about one requisition's funding request now and applies the decision, if any.</summary>
    /// <exception cref="NotFoundException">No such requisition (404).</exception>
    /// <exception cref="ValidationFailedException">It was never sent to Budget (422).</exception>
    /// <exception cref="BudgetServiceException">Budget failed (502).</exception>
    public async Task<RequisitionDto> SyncAsync(string requisitionNumber)
    {
        var requisition = await _requisitionRepository.GetByNumberAsync(requisitionNumber)
                          ?? throw new NotFoundException("Requisition", requisitionNumber);
        if (requisition.FundingRequestId == null)
            throw new ValidationFailedException("fundingRequest", $"{requisition.RequisitionNumber} has not been sent to Budget.");

        var fromBudget = (await _budget.ListFundingRequestsAsync()).FirstOrDefault(f => f.Id == requisition.FundingRequestId);
        Apply(requisition, fromBudget, _clock.UtcNow);
        await _requisitionRepository.SaveChangesAsync();
        return requisition.ToDto();
    }

    /// <summary>
    /// Checks every requisition still waiting on Budget (one call to Budget) and applies the decisions.
    /// Returns how many were decided. Used by the background sync.
    /// </summary>
    public async Task<int> SyncAllAsync(CancellationToken ct = default)
    {
        var waiting = await _requisitionRepository.ListAwaitingFundingDecisionAsync();
        if (waiting.Count == 0 || !_budget.IsConfigured) return 0;

        var fromBudget = (await _budget.ListFundingRequestsAsync(ct)).ToDictionary(f => f.Id);
        var now = _clock.UtcNow;
        var decided = waiting.Count(r => Apply(r, fromBudget.GetValueOrDefault(r.FundingRequestId!.Value), now));
        await _requisitionRepository.SaveChangesAsync();
        if (decided > 0) _logger.LogInformation("Applied {Count} funding decision(s) from Budget", decided);
        return decided;
    }

    /// <summary>
    /// Copies Budget's decision onto the requisition. Approved → budgetDecision Approved and status Approved;
    /// Rejected → Rejected and Rejected. A requisition HR has already moved on (e.g. Closed) keeps its status.
    /// Returns true when a decision was applied.
    /// </summary>
    private static bool Apply(Requisition requisition, BudgetFundingRequest? fromBudget, DateTime now)
    {
        requisition.FundingCheckedAtUtc = now;
        if (fromBudget == null || fromBudget.Status == FundingStatus.Pending) return false;

        var approved = fromBudget.Status == FundingStatus.Approved;
        requisition.FundingDecidedAtUtc = fromBudget.DecidedAtUtc ?? now;
        if (requisition.Status != RequisitionStatus.AwaitingBudget) return false;

        requisition.BudgetDecision = approved ? BudgetDecision.Approved : BudgetDecision.Rejected;
        requisition.Status = approved ? RequisitionStatus.Approved : RequisitionStatus.Rejected;
        return true;
    }

    /// <summary>Annual cost of one position: the average current salary in the same role, else in the department.</summary>
    private async Task<decimal?> EstimateAsync(Requisition requisition)
    {
        var today = _clock.Today;
        var average = await _compensationRepository.AverageCurrentSalaryAsync(requisition.RequestingDeptId, requisition.RoleTitle, today)
                      ?? await _compensationRepository.AverageCurrentSalaryAsync(requisition.RequestingDeptId, null, today);
        return average == null ? null : Math.Round(average.Value, 2);
    }

    /// <summary>"REQ-0000007: hire 2 x Primary Care Paramedic (EMS), reason VACANCY" — Budget's reference to us.</summary>
    private static string ReasonFor(Requisition requisition)
    {
        var text = $"{requisition.RequisitionNumber}: hire {requisition.Positions} x {requisition.RoleTitle} " +
                   $"({requisition.RequestingDepartment.Code})" +
                   (requisition.ReasonCode == null ? "" : $", reason {requisition.ReasonCode}");
        return text.Length <= MaxReasonLength ? text : text[..MaxReasonLength];
    }
}
