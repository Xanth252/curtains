using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == req.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username));
    }

    [Authorize]
    [HttpPut("account")]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        var username = req.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username)) return BadRequest(new { message = "Username is required." });
        if (await _db.Users.AnyAsync(u => u.Id != user.Id && u.Username.ToLower() == username.ToLower()))
            return Conflict(new { message = "That username is already in use." });

        if (!string.IsNullOrWhiteSpace(req.NewPassword) && req.NewPassword.Length < 8)
            return BadRequest(new { message = "New password must be at least 8 characters." });

        user.Username = username;
        if (!string.IsNullOrWhiteSpace(req.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        await _db.SaveChangesAsync();
        return Ok(new { username = user.Username });
    }
}
