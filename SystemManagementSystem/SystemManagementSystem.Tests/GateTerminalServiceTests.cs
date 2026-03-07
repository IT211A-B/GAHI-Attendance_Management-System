using SystemManagementSystem.DTOs.GateTerminals;
using SystemManagementSystem.Models.Enums;
using SystemManagementSystem.Services.Implementations;

namespace SystemManagementSystem.Tests;

public class GateTerminalServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsTerminal()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        var result = await svc.CreateAsync(new CreateGateTerminalRequest { Name = "Main Gate", Location = "Front Entrance", TerminalType = TerminalType.QRScanner });

        Assert.Equal("Main Gate", result.Name);
        Assert.Equal("Front Entrance", result.Location);
        Assert.Equal("QRScanner", result.TerminalType);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        await svc.CreateAsync(new CreateGateTerminalRequest { Name = "Main Gate", Location = "Front", TerminalType = TerminalType.QRScanner });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateGateTerminalRequest { Name = "Main Gate", Location = "Back", TerminalType = TerminalType.QRScanner }));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTerminal_ReturnsTerminal()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        var created = await svc.CreateAsync(new CreateGateTerminalRequest { Name = "Side Gate", Location = "Side", TerminalType = TerminalType.Manual });
        var result = await svc.GetByIdAsync(created.Id);

        Assert.Equal("Side Gate", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesTerminal()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        var created = await svc.CreateAsync(new CreateGateTerminalRequest { Name = "Gate A", Location = "North", TerminalType = TerminalType.QRScanner });
        var updated = await svc.UpdateAsync(created.Id, new UpdateGateTerminalRequest { Location = "South", IsActive = false });

        Assert.Equal("South", updated.Location);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTerminal_SoftDeletes()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        var created = await svc.CreateAsync(new CreateGateTerminalRequest { Name = "ToDelete", Location = "Loc", TerminalType = TerminalType.QRScanner });
        await svc.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new GateTerminalService(ctx);

        for (int i = 0; i < 4; i++)
            await svc.CreateAsync(new CreateGateTerminalRequest { Name = $"Gate{i}", Location = $"Loc{i}", TerminalType = TerminalType.QRScanner });

        var result = await svc.GetAllAsync(1, 2);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(4, result.TotalCount);
    }
}
