using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Implementation;
using Xunit;

namespace Teamownik.Tests.Services;

public class SettlementServiceTests
{
    [Fact]
    public async Task GetTotalToPayAsync_ShouldReturnCorrectSum()
    {
        var options = new DbContextOptionsBuilder<TeamownikDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TeamownikDbContext(options);
        
        var mockLogger = new Mock<ILogger<SettlementService>>();
        
        var userId = "user-1";
        context.Settlements.AddRange(
            new Settlement { PayerId = userId, Amount = 100, Status = Constants.SettlementStatus.Pending },
            new Settlement { PayerId = userId, Amount = 50, Status = Constants.SettlementStatus.Pending },
            new Settlement { PayerId = userId, Amount = 30, Status = Constants.SettlementStatus.Paid }
        );
        await context.SaveChangesAsync();

        var service = new SettlementService(context, mockLogger.Object);

        var total = await service.GetTotalToPayAsync(userId);

        Xunit.Assert.Equal(150, total);
    }
}