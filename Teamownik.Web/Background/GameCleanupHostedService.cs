using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Background;

public class GameCleanupHostedService : BackgroundService
{
    private readonly ILogger<GameCleanupHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); 

    public GameCleanupHostedService(
        ILogger<GameCleanupHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game Cleanup Service uruchomiony");

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return; 
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldGamesAsync();
            
                _logger.LogInformation($"Następne czyszczenie za {_interval.TotalMinutes} minut");
            
                await Task.Delay(_interval, stoppingToken); 
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {

                _logger.LogInformation("Proces czyszczenia przerwany na żądanie.");
                break; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas czyszczenia gier");
            
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task CleanupOldGamesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
        
        _logger.LogInformation("Rozpoczynam czyszczenie przeterminowanych gier...");
        await gameService.CleanupOldGamesAsync();
        _logger.LogInformation("Czyszczenie zakończone");
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game Cleanup Service zatrzymany");
        await base.StopAsync(stoppingToken);
    }
}