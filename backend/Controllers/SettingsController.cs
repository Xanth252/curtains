using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;
using backend.Models;
using System.Text.Json;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SettingsController(AppDbContext db) => _db = db;

    [HttpGet("business-profile")]
    public async Task<IActionResult> GetBusinessProfile()
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "business_profile");
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
        {
            try
            {
                var profile = JsonSerializer.Deserialize<BusinessProfileDto>(setting.Value);
                if (profile != null) return Ok(profile);
            }
            catch { }
        }

        return Ok(new BusinessProfileDto());
    }

    [HttpPost("business-profile")]
    public async Task<IActionResult> SaveBusinessProfile([FromBody] BusinessProfileDto dto)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "business_profile");
        var json = JsonSerializer.Serialize(dto);

        if (setting == null)
        {
            setting = new AppSetting { Key = "business_profile", Value = json, UpdatedAt = DateTime.UtcNow };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = json;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpGet("bank-details")]
    public async Task<IActionResult> GetBankDetails()
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "bank_details");
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
        {
            try
            {
                var bank = JsonSerializer.Deserialize<BankDetailsDto>(setting.Value);
                if (bank != null) return Ok(bank);
            }
            catch { }
        }

        return Ok(new BankDetailsDto());
    }

    [HttpPost("bank-details")]
    public async Task<IActionResult> SaveBankDetails([FromBody] BankDetailsDto dto)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "bank_details");
        var json = JsonSerializer.Serialize(dto);

        if (setting == null)
        {
            setting = new AppSetting { Key = "bank_details", Value = json, UpdatedAt = DateTime.UtcNow };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = json;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null) return NotFound();
        return Ok(new { key = setting.Key, value = setting.Value, updatedAt = setting.UpdatedAt });
    }

    [HttpPost("{key}")]
    public async Task<IActionResult> SaveByKey(string key, [FromBody] string value)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new AppSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { key = setting.Key, value = setting.Value });
    }
}
