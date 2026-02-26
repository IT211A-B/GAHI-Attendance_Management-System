using SystemManagementSystem.DTOs.BusinessRules;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Implementations;

namespace SystemManagementSystem.Tests;

public class BusinessRuleServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsRule()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new BusinessRuleService(ctx);

        var result = await svc.CreateAsync(new CreateBusinessRuleRequest
        {
            RuleKey = "MORNING_CUTOFF_TIME",
            RuleValue = "08:00:00",
            Description = "Morning cutoff"
        });

        Assert.Equal("MORNING_CUTOFF_TIME", result.RuleKey);
        Assert.Equal("08:00:00", result.RuleValue);
        Assert.Null(result.DepartmentId);
    }

    [Fact]
    public async Task CreateAsync_DuplicateKeyAndScope_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new BusinessRuleService(ctx);

        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "KEY1", RuleValue = "V1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "KEY1", RuleValue = "V2" }));
    }

    [Fact]
    public async Task CreateAsync_SameKeyDifferentDepartment_Succeeds()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = new Department { Name = "ENG", Code = "ENG" };
        ctx.Departments.Add(dept);
        ctx.SaveChanges();

        var svc = new BusinessRuleService(ctx);

        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "GRACE_PERIOD_MINUTES", RuleValue = "15" });
        var deptRule = await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "GRACE_PERIOD_MINUTES", RuleValue = "10", DepartmentId = dept.Id });

        Assert.Equal(dept.Id, deptRule.DepartmentId);
    }

    [Fact]
    public async Task GetRuleValueAsync_GlobalRule_ReturnsValue()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new BusinessRuleService(ctx);

        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "TEST_RULE", RuleValue = "42" });

        var value = await svc.GetRuleValueAsync("TEST_RULE");
        Assert.Equal("42", value);
    }

    [Fact]
    public async Task GetRuleValueAsync_DepartmentSpecificOverridesGlobal()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = new Department { Name = "COL", Code = "COL" };
        ctx.Departments.Add(dept);
        ctx.SaveChanges();

        var svc = new BusinessRuleService(ctx);

        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "GRACE", RuleValue = "15" });
        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "GRACE", RuleValue = "10", DepartmentId = dept.Id });

        var value = await svc.GetRuleValueAsync("GRACE", dept.Id);
        Assert.Equal("10", value);
    }

    [Fact]
    public async Task GetRuleValueAsync_NoDeptRule_FallsBackToGlobal()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = new Department { Name = "COL", Code = "COL" };
        ctx.Departments.Add(dept);
        ctx.SaveChanges();

        var svc = new BusinessRuleService(ctx);

        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "CUTOFF", RuleValue = "08:00" });

        var value = await svc.GetRuleValueAsync("CUTOFF", dept.Id);
        Assert.Equal("08:00", value);
    }

    [Fact]
    public async Task GetRuleValueAsync_NonExistentRule_ReturnsNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new BusinessRuleService(ctx);

        var value = await svc.GetRuleValueAsync("NONEXISTENT");
        Assert.Null(value);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_Updates()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new BusinessRuleService(ctx);

        var created = await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "UPD", RuleValue = "old" });
        var updated = await svc.UpdateAsync(created.Id, new UpdateBusinessRuleRequest { RuleValue = "new" });

        Assert.Equal("new", updated.RuleValue);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRule_SoftDeletes()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = new BusinessRuleService(ctx);

        var created = await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "DEL", RuleValue = "v" });
        await svc.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task GetAllAsync_WithDepartmentFilter_FiltersCorrectly()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = new Department { Name = "FLT", Code = "FLT" };
        ctx.Departments.Add(dept);
        ctx.SaveChanges();

        var svc = new BusinessRuleService(ctx);

        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "GLOBAL", RuleValue = "v" });
        await svc.CreateAsync(new CreateBusinessRuleRequest { RuleKey = "DEPT", RuleValue = "v", DepartmentId = dept.Id });

        var result = await svc.GetAllAsync(1, 10, dept.Id);
        Assert.Single(result.Items);
        Assert.Equal("DEPT", result.Items[0].RuleKey);
    }
}
