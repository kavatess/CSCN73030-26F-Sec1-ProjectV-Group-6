/**
 * @file BudgetSyncService.cs
 * @brief Background loop that reads Budget's funding decisions every few minutes.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Budget;

/// <summary>
/// Budget decides funding requests in its own system and has no callback to Staff/HR, so this loop asks Budget
/// for the decisions on requisitions still AwaitingBudget. Off when Budget:BaseUrl is empty or
/// Budget:BackgroundSync is false.
/// </summary>
public class BudgetSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BudgetOptions _options;
    private readonly ILogger<BudgetSyncService> _logger;

    /// <summary>Creates the loop.</summary>
    public BudgetSyncService(IServiceScopeFactory scopeFactory, IOptions<BudgetOptions> options, ILogger<BudgetSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured || !_options.BackgroundSync) return;

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.SyncIntervalMinutes));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BudgetFundingService>().SyncAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Budget decision check failed; retrying in {Interval}.", interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
