using SystemManagementSystem.DTOs.AuditLogs;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Implementations;

namespace SystemManagementSystem.Tests;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_CreatesAuditLogEntry()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new AuditLogService(ctx);

        var userId = Guid.NewGuid();
        await svc.LogAsync("Create", "Student", Guid.NewGuid().ToString(), null, "{\"Name\":\"John\"}", userId);

        var logs = await svc.GetLogsAsync(new AuditLogFilterRequest { Page = 1, PageSize = 10 });

        Assert.Single(logs.Items);
        Assert.Equal("Create", logs.Items[0].Action);
        Assert.Equal("Student", logs.Items[0].EntityName);
        Assert.Equal(userId, logs.Items[0].PerformedByUserId);
    }

    [Fact]
    public async Task LogAsync_WithOldAndNewValues_StoresCorrectly()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new AuditLogService(ctx);

        var entityId = Guid.NewGuid().ToString();
        await svc.LogAsync("Update", "Student", entityId, "{\"Name\":\"Old\"}", "{\"Name\":\"New\"}", null);

        var logs = await svc.GetLogsAsync(new AuditLogFilterRequest { EntityName = "Student", Page = 1, PageSize = 10 });

        Assert.Single(logs.Items);
        Assert.Equal("{\"Name\":\"Old\"}", logs.Items[0].OldValues);
        Assert.Equal("{\"Name\":\"New\"}", logs.Items[0].NewValues);
    }

    [Fact]
    public async Task GetLogsAsync_FilterByAction_ReturnsFilteredResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new AuditLogService(ctx);

        await svc.LogAsync("Create", "Student", "1", null, "{}", null);
        await svc.LogAsync("Update", "Student", "1", "{}", "{}", null);
        await svc.LogAsync("Delete", "Student", "1", "{}", null, null);

        var creates = await svc.GetLogsAsync(new AuditLogFilterRequest { Action = "Create", Page = 1, PageSize = 10 });
        Assert.Single(creates.Items);

        var deletes = await svc.GetLogsAsync(new AuditLogFilterRequest { Action = "Delete", Page = 1, PageSize = 10 });
        Assert.Single(deletes.Items);
    }

    [Fact]
    public async Task GetLogsAsync_FilterByEntityName_ReturnsFilteredResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new AuditLogService(ctx);

        await svc.LogAsync("Create", "Student", "1", null, "{}", null);
        await svc.LogAsync("Create", "Department", "2", null, "{}", null);

        var studentLogs = await svc.GetLogsAsync(new AuditLogFilterRequest { EntityName = "Student", Page = 1, PageSize = 10 });
        Assert.Single(studentLogs.Items);
    }

    [Fact]
    public async Task GetLogsAsync_Pagination_Works()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new AuditLogService(ctx);

        for (int i = 0; i < 5; i++)
            await svc.LogAsync("Create", "Entity", i.ToString(), null, "{}", null);

        var page1 = await svc.GetLogsAsync(new AuditLogFilterRequest { Page = 1, PageSize = 2 });
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalCount);

        var page2 = await svc.GetLogsAsync(new AuditLogFilterRequest { Page = 2, PageSize = 2 });
        Assert.Equal(2, page2.Items.Count);
    }

    [Fact]
    public async Task GetLogsAsync_FilterByDateRange_Works()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new AuditLogService(ctx);

        await svc.LogAsync("Create", "Student", "1", null, "{}", null);

        var result = await svc.GetLogsAsync(new AuditLogFilterRequest
        {
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddHours(1),
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.Items);
    }
}
