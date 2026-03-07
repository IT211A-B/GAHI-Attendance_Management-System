using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.BusinessRules;

namespace SystemManagementSystem.Services.Interfaces;

public interface IBusinessRuleService
{
    Task<PagedResult<BusinessRuleResponse>> GetAllAsync(int page, int pageSize, Guid? departmentId);
    Task<BusinessRuleResponse> GetByIdAsync(Guid id);
    Task<BusinessRuleResponse> CreateAsync(CreateBusinessRuleRequest request);
    Task<BusinessRuleResponse> UpdateAsync(Guid id, UpdateBusinessRuleRequest request);
    Task DeleteAsync(Guid id);
    Task<string?> GetRuleValueAsync(string ruleKey, Guid? departmentId = null);
}
