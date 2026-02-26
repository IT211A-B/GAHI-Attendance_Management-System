using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.BusinessRules;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class BusinessRuleService : IBusinessRuleService
{
    private readonly ApplicationDbContext _context;

    public BusinessRuleService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<BusinessRuleResponse>> GetAllAsync(int page, int pageSize, Guid? departmentId)
    {
        var query = _context.BusinessRules
            .Include(b => b.Department)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(b => b.DepartmentId == departmentId.Value);

        query = query.OrderBy(b => b.RuleKey);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BusinessRuleResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BusinessRuleResponse> GetByIdAsync(Guid id)
    {
        var rule = await _context.BusinessRules
            .Include(b => b.Department)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException($"Business rule with ID {id} not found.");

        return MapToResponse(rule);
    }

    public async Task<BusinessRuleResponse> CreateAsync(CreateBusinessRuleRequest request)
    {
        if (await _context.BusinessRules.AnyAsync(b => b.RuleKey == request.RuleKey && b.DepartmentId == request.DepartmentId))
            throw new InvalidOperationException($"Business rule '{request.RuleKey}' already exists for this scope.");

        if (request.DepartmentId.HasValue && !await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
            throw new KeyNotFoundException($"Department with ID {request.DepartmentId} not found.");

        var rule = new BusinessRule
        {
            RuleKey = request.RuleKey,
            RuleValue = request.RuleValue,
            Description = request.Description,
            DepartmentId = request.DepartmentId
        };

        _context.BusinessRules.Add(rule);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(rule.Id);
    }

    public async Task<BusinessRuleResponse> UpdateAsync(Guid id, UpdateBusinessRuleRequest request)
    {
        var rule = await _context.BusinessRules.FindAsync(id)
            ?? throw new KeyNotFoundException($"Business rule with ID {id} not found.");

        if (request.RuleValue != null) rule.RuleValue = request.RuleValue;
        if (request.Description != null) rule.Description = request.Description;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await _context.BusinessRules.FindAsync(id)
            ?? throw new KeyNotFoundException($"Business rule with ID {id} not found.");

        rule.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<string?> GetRuleValueAsync(string ruleKey, Guid? departmentId = null)
    {
        // Try department-specific rule first, then fall back to global
        if (departmentId.HasValue)
        {
            var deptRule = await _context.BusinessRules
                .FirstOrDefaultAsync(b => b.RuleKey == ruleKey && b.DepartmentId == departmentId.Value);
            if (deptRule != null) return deptRule.RuleValue;
        }

        var globalRule = await _context.BusinessRules
            .FirstOrDefaultAsync(b => b.RuleKey == ruleKey && b.DepartmentId == null);
        return globalRule?.RuleValue;
    }

    private static BusinessRuleResponse MapToResponse(BusinessRule b) => new()
    {
        Id = b.Id,
        RuleKey = b.RuleKey,
        RuleValue = b.RuleValue,
        Description = b.Description,
        DepartmentId = b.DepartmentId,
        DepartmentName = b.Department?.Name,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
