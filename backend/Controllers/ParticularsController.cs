using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParticularsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ParticularsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var query = _db.SavedParticulars.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Particular.ToLower().Contains(search.ToLower()));
        }

        var list = await query
            .OrderByDescending(p => p.UsageCount)
            .ThenBy(p => p.Particular)
            .ToListAsync();

        return Ok(list.Select(p => new SavedParticularDto
        {
            Id = p.Id,
            Particular = p.Particular,
            DefaultUnit = p.DefaultUnit,
            DefaultRate = p.DefaultRate,
            UsageCount = p.UsageCount,
            LastUsedAt = p.LastUsedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] SavedParticularCreateDto dto)
    {
        var name = dto.Particular?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Particular name cannot be empty");

        var existing = await _db.SavedParticulars.FirstOrDefaultAsync(p => p.Particular.ToLower() == name.ToLower());
        if (existing != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.DefaultUnit)) existing.DefaultUnit = dto.DefaultUnit.Trim();
            if (dto.DefaultRate > 0) existing.DefaultRate = dto.DefaultRate;
            existing.UsageCount++;
            existing.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new SavedParticularDto
            {
                Id = existing.Id,
                Particular = existing.Particular,
                DefaultUnit = existing.DefaultUnit,
                DefaultRate = existing.DefaultRate,
                UsageCount = existing.UsageCount,
                LastUsedAt = existing.LastUsedAt
            });
        }

        var newParticular = new SavedParticular
        {
            Particular = name,
            DefaultUnit = string.IsNullOrWhiteSpace(dto.DefaultUnit) ? "mtr" : dto.DefaultUnit.Trim(),
            DefaultRate = dto.DefaultRate,
            UsageCount = 1,
            LastUsedAt = DateTime.UtcNow
        };

        _db.SavedParticulars.Add(newParticular);
        await _db.SaveChangesAsync();

        return Ok(new SavedParticularDto
        {
            Id = newParticular.Id,
            Particular = newParticular.Particular,
            DefaultUnit = newParticular.DefaultUnit,
            DefaultRate = newParticular.DefaultRate,
            UsageCount = newParticular.UsageCount,
            LastUsedAt = newParticular.LastUsedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SavedParticularCreateDto dto)
    {
        var name = dto.Particular?.Trim() ?? "";
        var unit = dto.DefaultUnit?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Product name cannot be empty");
        if (string.IsNullOrWhiteSpace(unit)) return BadRequest("Product unit cannot be empty");

        var product = await _db.SavedParticulars.FindAsync(id);
        if (product == null) return NotFound();
        var duplicate = await _db.SavedParticulars.AnyAsync(p => p.Id != id && p.Particular.ToLower() == name.ToLower());
        if (duplicate) return Conflict("A product with this name already exists");

        product.Particular = name;
        product.DefaultUnit = unit;
        product.DefaultRate = dto.DefaultRate;
        await _db.SaveChangesAsync();
        return Ok(new SavedParticularDto
        {
            Id = product.Id,
            Particular = product.Particular,
            DefaultUnit = product.DefaultUnit,
            DefaultRate = product.DefaultRate,
            UsageCount = product.UsageCount,
            LastUsedAt = product.LastUsedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.SavedParticulars.FindAsync(id);
        if (p == null) return NotFound();
        _db.SavedParticulars.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("units")]
    public async Task<IActionResult> GetUnits()
    {
        var defaultUnits = new List<string> { "mtr", "ft", "pcs", "kg", "sq ft", "roll", "set", "pair", "inches", "cm", "pkt", "box" };
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "saved_units");
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
        {
            try
            {
                var customUnits = System.Text.Json.JsonSerializer.Deserialize<List<string>>(setting.Value);
                if (customUnits != null)
                {
                    foreach (var u in customUnits)
                    {
                        if (!defaultUnits.Contains(u, StringComparer.OrdinalIgnoreCase))
                            defaultUnits.Add(u);
                    }
                }
            }
            catch { }
        }

        // Also gather any units from SavedParticulars
        var particularUnits = await _db.SavedParticulars.Select(p => p.DefaultUnit).Distinct().ToListAsync();
        foreach (var u in particularUnits)
        {
            if (!string.IsNullOrWhiteSpace(u) && !defaultUnits.Contains(u, StringComparer.OrdinalIgnoreCase))
                defaultUnits.Add(u);
        }

        return Ok(defaultUnits);
    }

    [HttpPost("units")]
    public async Task<IActionResult> AddUnit([FromBody] string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return BadRequest("Unit cannot be empty");
        var cleanUnit = unit.Trim().ToLower();

        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "saved_units");
        var unitsList = new List<string>();
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
        {
            try { unitsList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(setting.Value) ?? new List<string>(); }
            catch { }
        }

        if (!unitsList.Contains(cleanUnit, StringComparer.OrdinalIgnoreCase))
        {
            unitsList.Add(cleanUnit);
            if (setting == null)
            {
                setting = new AppSetting { Key = "saved_units", Value = System.Text.Json.JsonSerializer.Serialize(unitsList), UpdatedAt = DateTime.UtcNow };
                _db.AppSettings.Add(setting);
            }
            else
            {
                setting.Value = System.Text.Json.JsonSerializer.Serialize(unitsList);
                setting.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true, unit = cleanUnit });
    }
}
