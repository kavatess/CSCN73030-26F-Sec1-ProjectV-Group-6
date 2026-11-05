using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Hire requisitions. Termination requisitions are out of scope for this release.</summary>
public class RequisitionService(
    RequisitionRepository requisitionRepository,
    DepartmentRepository departmentRepository,
    Clock clock)
{
    public const int MaxIdempotencyKeyLength = 100;
    public const int MaxPositions = 100;

    /// <summary>Result of a create: the requisition, and whether it already existed for this key.</summary>
    public record CreateResult(RequisitionDto Requisition, bool Replayed);

    /// <summary>
    /// Business rule 3: creating with an Idempotency-Key that was already used returns the original
    /// requisition instead of a second one. Re-using a key for a different request is a 409.
    /// </summary>
    public async Task<CreateResult> CreateAsync(string? idempotencyKey, CreateRequisitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new BadRequestException("The Idempotency-Key header is required. Send a unique value (e.g. a GUID) per new requisition.");
        idempotencyKey = idempotencyKey.Trim();
        if (idempotencyKey.Length > MaxIdempotencyKeyLength)
            throw new BadRequestException($"The Idempotency-Key header must be at most {MaxIdempotencyKeyLength} characters.");

        var original = await requisitionRepository.GetByIdempotencyKeyAsync(idempotencyKey);
        if (original != null)
            return Replay(original, request, idempotencyKey);

        var errors = new ValidationErrors();
        errors.AddIf(request.Type == RequisitionType.Termination, "type",
            "Only Hire requisitions are supported in this release.");
        errors.Required("departmentCode", request.DepartmentCode, 10);
        errors.Required("roleTitle", request.RoleTitle);
        errors.Required("positions", request.Positions);
        errors.AddIf(request.Positions is < 1 or > MaxPositions, "positions", $"Positions must be between 1 and {MaxPositions}.");
        errors.MaxLength("reasonCode", request.ReasonCode, 50);

        Department? department = null;
        if (!string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            department = await departmentRepository.GetByCodeAsync(request.DepartmentCode);
            errors.AddIf(department == null, "departmentCode", $"Unknown department code '{request.DepartmentCode}'.");
        }
        errors.ThrowIfAny();

        var requisition = new Requisition
        {
            RequisitionNumber = await requisitionRepository.NextRequisitionNumberAsync(),
            RequestingDeptId = department!.DepartmentId,
            Type = RequisitionType.Hire,
            RoleTitle = request.RoleTitle!.Trim(),
            Positions = request.Positions!.Value,
            PositionsFilled = 0,
            Status = RequisitionStatus.Received,
            ReasonCode = Text.TrimToNull(request.ReasonCode),
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = clock.UtcNow,
        };
        requisitionRepository.Add(requisition);

        try
        {
            await requisitionRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Two requests with the same key arrived at once and the other one won. Answer as a replay.
            requisitionRepository.Detach(requisition);
            var winner = await requisitionRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            if (winner == null) throw;
            return Replay(winner, request, idempotencyKey);
        }

        var saved = await requisitionRepository.GetByNumberAsync(requisition.RequisitionNumber);
        return new CreateResult(saved!.ToDto(), Replayed: false);
    }

    public async Task<List<RequisitionDto>> ListAsync(RequisitionQuery query)
    {
        int? departmentId = null;
        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = await departmentRepository.GetByCodeAsync(query.Department)
                             ?? throw new ValidationFailedException("department", $"Unknown department code '{query.Department}'.");
            departmentId = department.DepartmentId;
        }

        var requisitions = await requisitionRepository.ListAsync(query.Type, query.Status, departmentId);
        return requisitions.Select(r => r.ToDto()).ToList();
    }

    public async Task<RequisitionDto> GetAsync(string requisitionNumber)
    {
        var requisition = await requisitionRepository.GetByNumberAsync(requisitionNumber)
                          ?? throw new NotFoundException("Requisition", requisitionNumber);
        return requisition.ToDto();
    }

    /// <summary>Records a status change, budget decision and/or filled count by hand.</summary>
    public async Task<RequisitionDto> UpdateAsync(string requisitionNumber, UpdateRequisitionRequest request)
    {
        var requisition = await requisitionRepository.GetByNumberAsync(requisitionNumber)
                          ?? throw new NotFoundException("Requisition", requisitionNumber);

        var errors = new ValidationErrors();
        errors.AddIf(request is { Status: null, BudgetDecision: null, PositionsFilled: null }, "body",
            "Provide at least one of status, budgetDecision or positionsFilled.");

        var newStatus = request.Status ?? requisition.Status;
        var newDecision = request.BudgetDecision ?? requisition.BudgetDecision;
        var newFilled = request.PositionsFilled ?? requisition.PositionsFilled;

        if (request.Status != null && request.Status != requisition.Status &&
            !RequisitionWorkflow.CanMove(requisition.Status, request.Status.Value))
        {
            var allowed = RequisitionWorkflow.NextStatuses(requisition.Status);
            errors.Add("status", allowed.Count == 0
                ? $"{requisition.Status} is a final status; the requisition can no longer change."
                : $"Cannot move from {requisition.Status} to {request.Status}. Allowed: {string.Join(", ", allowed)}.");
        }
        errors.AddIf(request.Status == RequisitionStatus.Approved && newDecision != BudgetDecision.Approved, "budgetDecision",
            "A requisition can only be Approved once Budget has approved it (budgetDecision = Approved).");
        errors.AddIf(newFilled < 0 || newFilled > requisition.Positions, "positionsFilled",
            $"PositionsFilled must be between 0 and {requisition.Positions}.");
        errors.AddIf(request.Status == RequisitionStatus.Completed && newFilled != requisition.Positions, "positionsFilled",
            "A requisition can only be Completed when every position is filled.");
        errors.ThrowIfAny();

        requisition.Status = newStatus;
        requisition.BudgetDecision = newDecision;
        requisition.PositionsFilled = newFilled;
        await requisitionRepository.SaveChangesAsync();

        return requisition.ToDto();
    }

    private static CreateResult Replay(Requisition original, CreateRequisitionRequest request, string idempotencyKey)
    {
        var samePayload =
            (request.Type ?? RequisitionType.Hire) == original.Type &&
            string.Equals(request.DepartmentCode?.Trim(), original.RequestingDepartment.Code, StringComparison.OrdinalIgnoreCase) &&
            request.RoleTitle?.Trim() == original.RoleTitle &&
            request.Positions == original.Positions &&
            Text.TrimToNull(request.ReasonCode) == original.ReasonCode;

        if (!samePayload)
            throw new ConflictException(
                $"Idempotency-Key '{idempotencyKey}' was already used for requisition {original.RequisitionNumber} " +
                "with a different request body. Use a new key for a new requisition.");

        return new CreateResult(original.ToDto(), Replayed: true);
    }
}
