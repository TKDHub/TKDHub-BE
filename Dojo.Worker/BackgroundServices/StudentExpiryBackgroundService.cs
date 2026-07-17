using Dojo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dojo.Worker.BackgroundServices;

/// <summary>
/// Runs the student-expiry sweep once at startup (so a redeploy never skips a day) and then
/// every 24 hours at UTC midnight. All the actual work lives in
/// <see cref="IStudentExpiryProcessor"/> — this class is just the timer/scope wrapper, since
/// hosted services are registered as singletons but repositories/DbContext are scoped.
/// </summary>
public sealed class StudentExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory                          _scopeFactory;
    private readonly ILogger<StudentExpiryBackgroundService>       _logger;

    public StudentExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<StudentExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IStudentExpiryProcessor>();

            var processed = await processor.ProcessExpiredStudentsAsync(cancellationToken);

            if (processed > 0)
                _logger.LogInformation("Student expiry sweep deactivated {Count} student(s)", processed);
        }
        catch (Exception ex)
        {
            // A failed sweep must never crash the host — there's always tomorrow's run.
            _logger.LogError(ex, "Student expiry sweep failed");
        }
    }
}
