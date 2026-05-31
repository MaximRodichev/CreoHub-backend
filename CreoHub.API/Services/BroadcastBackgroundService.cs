using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreoHub.API.Services;

/// <summary>
/// Background service that polls for pending broadcast jobs and delivers them
/// at a rate-limited pace (~2s per message) to avoid Telegram rate limits.
/// </summary>
public class BroadcastBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastBackgroundService> _logger;

    // Poll interval when no jobs are pending
    private static readonly TimeSpan PollInterval    = TimeSpan.FromSeconds(10);
    // Delay between individual messages within a broadcast
    private static readonly TimeSpan MessageDelay    = TimeSpan.FromSeconds(2);

    public BroadcastBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BroadcastBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastBackgroundService started.");

        // Сброс джобов, застрявших в статусе "running" из-за предыдущего краша
        try
        {
            using var startScope = _scopeFactory.CreateScope();
            var repo = startScope.ServiceProvider.GetRequiredService<IBroadcastJobRepository>();
            var uow  = startScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await repo.ResetStuckRunningJobsAsync(stoppingToken);
            await uow.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BroadcastBackgroundService: could not reset stuck jobs on startup");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BroadcastBackgroundService: error in main loop");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        using var scope           = _scopeFactory.CreateScope();
        var broadcastRepo         = scope.ServiceProvider.GetRequiredService<IBroadcastJobRepository>();
        var accountRepo           = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var notificationService   = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var unitOfWork            = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingJobs = await broadcastRepo.GetPendingAsync(ct);
        if (pendingJobs.Count == 0) return;

        _logger.LogInformation("BroadcastBackgroundService: found {Count} pending job(s)", pendingJobs.Count);

        // Get all users with contact info once for all jobs
        var usersWithContact = await accountRepo.GetUsersWithContactInfoAsync(ct);

        foreach (var job in pendingJobs)
        {
            await RunJobAsync(job, usersWithContact, broadcastRepo, notificationService, unitOfWork, ct);
        }
    }

    private async Task RunJobAsync(
        BroadcastJob job,
        List<(long? TelegramId, string? Email, bool NotifyOnPurchase, bool NotifyOnModeration)> users,
        IBroadcastJobRepository broadcastRepo,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        _logger.LogInformation("BroadcastBackgroundService: starting job {JobId}", job.Id);

        try
        {
            job.Start(users.Count);
            broadcastRepo.Update(job);
            await unitOfWork.SaveChangesAsync(ct);

            foreach (var (telegramId, email, _, _) in users)
            {
                if (ct.IsCancellationRequested) break;

                var sent = await notificationService.SendAsync(telegramId, email, job.Message, ct);
                if (sent) job.RecordSent();
                else      job.RecordFailed();

                broadcastRepo.Update(job);
                await unitOfWork.SaveChangesAsync(ct);

                // Rate-limit: ~2s between messages to avoid Telegram 429
                await Task.Delay(MessageDelay, ct);
            }

            job.Complete();
            broadcastRepo.Update(job);
            await unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "BroadcastBackgroundService: job {JobId} done. Sent={Sent}, Failed={Failed}",
                job.Id, job.SentCount, job.FailedCount);
        }
        catch (OperationCanceledException)
        {
            // App is shutting down — leave job as Running (will be retried on restart)
            _logger.LogWarning("BroadcastBackgroundService: job {JobId} interrupted by cancellation", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BroadcastBackgroundService: job {JobId} failed", job.Id);
            try
            {
                job.Fail(ex.Message[..Math.Min(ex.Message.Length, 500)]);
                broadcastRepo.Update(job);
                await unitOfWork.SaveChangesAsync(default);
            }
            catch { }
        }
    }
}
