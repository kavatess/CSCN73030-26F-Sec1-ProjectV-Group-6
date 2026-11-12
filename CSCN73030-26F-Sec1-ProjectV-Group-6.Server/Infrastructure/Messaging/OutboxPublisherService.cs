/**
 * @file OutboxPublisherService.cs
 * @brief Background loop that runs the OutboxDispatcher every few seconds.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Messaging;

/// <summary>Runs <see cref="OutboxDispatcher.DispatchPendingAsync"/> on a timer, each pass in its own DI scope.</summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessagingOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;

    /// <summary>Creates the service.</summary>
    /// <param name="scopeFactory">Creates a scope (and DbContext) per pass.</param>
    /// <param name="options">Messaging settings.</param>
    /// <param name="logger">Logger.</param>
    public OutboxPublisherService(IServiceScopeFactory scopeFactory, IOptions<MessagingOptions> options, ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Publishes pending events until the app stops. A pass that fails is logged and retried on the next tick.</summary>
    /// <param name="stoppingToken">Signalled when the app stops.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.BackgroundPublishing) return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Outbox pass failed; retrying in {Seconds}s.", _options.PollIntervalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
