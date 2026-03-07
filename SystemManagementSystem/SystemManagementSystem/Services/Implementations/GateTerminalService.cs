using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.GateTerminals;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class GateTerminalService : IGateTerminalService
{
    private readonly ApplicationDbContext _context;

    public GateTerminalService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<GateTerminalResponse>> GetAllAsync(int page, int pageSize)
    {
        var query = _context.GateTerminals.OrderBy(g => g.Name);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GateTerminalResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<GateTerminalResponse> GetByIdAsync(Guid id)
    {
        var terminal = await _context.GateTerminals
            .FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException($"Gate terminal with ID {id} not found.");

        return MapToResponse(terminal);
    }

    public async Task<GateTerminalResponse> CreateAsync(CreateGateTerminalRequest request)
    {
        if (await _context.GateTerminals.AnyAsync(g => g.Name == request.Name))
            throw new InvalidOperationException($"Gate terminal '{request.Name}' already exists.");

        var terminal = new GateTerminal
        {
            Name = request.Name,
            Location = request.Location,
            TerminalType = request.TerminalType
        };

        _context.GateTerminals.Add(terminal);
        await _context.SaveChangesAsync();
        return MapToResponse(terminal);
    }

    public async Task<GateTerminalResponse> UpdateAsync(Guid id, UpdateGateTerminalRequest request)
    {
        var terminal = await _context.GateTerminals.FindAsync(id)
            ?? throw new KeyNotFoundException($"Gate terminal with ID {id} not found.");

        if (request.Name != null)
        {
            if (await _context.GateTerminals.AnyAsync(g => g.Name == request.Name && g.Id != id))
                throw new InvalidOperationException($"Gate terminal '{request.Name}' already exists.");
            terminal.Name = request.Name;
        }

        if (request.Location != null) terminal.Location = request.Location;
        if (request.TerminalType.HasValue) terminal.TerminalType = request.TerminalType.Value;
        if (request.IsActive.HasValue) terminal.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();
        return MapToResponse(terminal);
    }

    public async Task DeleteAsync(Guid id)
    {
        var terminal = await _context.GateTerminals.FindAsync(id)
            ?? throw new KeyNotFoundException($"Gate terminal with ID {id} not found.");

        terminal.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private static GateTerminalResponse MapToResponse(GateTerminal g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Location = g.Location,
        TerminalType = g.TerminalType.ToString(),
        IsActive = g.IsActive,
        CreatedAt = g.CreatedAt,
        UpdatedAt = g.UpdatedAt
    };
}
