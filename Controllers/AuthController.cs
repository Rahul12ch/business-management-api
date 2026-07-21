using client.Data;
using client.Helpers;
using client.Models;
using client.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly SupabaseStorageService _supabaseStorageService;

    public AuthController( AppDbContext context, IConfiguration configuration, SupabaseStorageService supabaseStorageService)
    {
        _context = context;
        _configuration = configuration;
        _supabaseStorageService = supabaseStorageService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(x => new { x.Id, x.Username, x.Email })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        user.Username = user.Username.Trim().ToLower();
        user.Email = user.Email.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(user.Username))
            return BadRequest(new { message = "Username is required." });

        if (user.Username.Length < 3 || user.Username.Length > 20)
            return BadRequest(new { message = "Username must be between 3 and 20 characters." });

        if (!System.Text.RegularExpressions.Regex.IsMatch(user.Username, @"^[a-z0-9_]+$"))
            return BadRequest(new { message = "Username can contain only lowercase letters, numbers and underscores." });

        if (string.IsNullOrWhiteSpace(user.Email))
            return BadRequest(new { message = "Email is required." });

        if (!new EmailAddressAttribute().IsValid(user.Email))
            return BadRequest(new { message = "Please enter a valid email address." });

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return BadRequest(new { message = "Password is required." });

        if (!System.Text.RegularExpressions.Regex.IsMatch(user.PasswordHash,
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#])[A-Za-z\d@$!%*?&.#]{8,}$"))
            return BadRequest(new { message = "Password must contain uppercase, lowercase, number and special character." });

        if (await _context.Users.AnyAsync(x => x.Username == user.Username))
            return Conflict(new { message = "Username already exists." });

        if (await _context.Users.AnyAsync(x => x.Email == user.Email))
            return Conflict(new { message = "An account with this email already exists." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.CreatedDate = DateTimeHelper.UtcNow();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User registered successfully.",
            userId = user.Id,
            username = user.Username,
            email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(User model)
    {
        model.Email = model.Email.Trim().ToLower();
        var user = await _context.Users .FirstOrDefaultAsync(x => x.Email == model.Email);
        if (user == null)
        {
            return Unauthorized(new
            { message = "No account found with this email. Please create an account." });
        }
        bool valid = BCrypt.Net.BCrypt.Verify( model.PasswordHash, user.PasswordHash);
        if (!valid)
        {
            return Unauthorized(new
            { message = "Incorrect password." });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials( key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken( issuer: _configuration["Jwt:Issuer"], audience: _configuration["Jwt:Audience"],
            claims: claims, expires: DateTimeHelper.UtcNow().AddDays(7), signingCredentials: creds);
        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            username = user.Username, email = user.Email, userId = user.Id
        });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _context.Users .FirstOrDefaultAsync(x => x.Id == int.Parse(userId));
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id, user.Username, user.Email, user.ProfileImage,
            CreatedDate = DateTimeHelper.ToIndia(user.CreatedDate)
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileModel model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _context.Users .FirstOrDefaultAsync(x => x.Id == int.Parse(userId));
        if (user == null) return NotFound();
        model.Username = model.Username.Trim().ToLower();
        model.Email = model.Email.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(model.Username))
            return BadRequest(new { message = "Username is required." });

        if (model.Username.Length < 3 || model.Username.Length > 20)
            return BadRequest(new { message = "Username must be between 3 and 20 characters." });

        if (!System.Text.RegularExpressions.Regex.IsMatch(model.Username, @"^[a-z0-9_]+$"))
            return BadRequest(new { message = "Username can contain only lowercase letters, numbers and underscores." });

        if (string.IsNullOrWhiteSpace(model.Email))
            return BadRequest(new { message = "Email is required." });

        if (!new EmailAddressAttribute().IsValid(model.Email))
            return BadRequest(new { message = "Please enter a valid email address." });

        if (await _context.Users.AnyAsync(x => x.Username == model.Username && x.Id != user.Id))
            return Conflict(new { message = "Username already exists." });

        if (await _context.Users.AnyAsync(x => x.Email == model.Email && x.Id != user.Id))
            return Conflict(new { message = "Email already exists." });
        user.Username = model.Username;
        user.Email = model.Email;

        string? oldImage = user.ProfileImage;
        if (!string.IsNullOrWhiteSpace(model.ProfileImage) &&  model.ProfileImage != user.ProfileImage)
        {  user.ProfileImage = model.ProfileImage; }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                return BadRequest(new
                { message = "Current password is required." });
            }
            if (!BCrypt.Net.BCrypt.Verify( model.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new
                { message = "Current password is incorrect." });
            }
            if (model.NewPassword.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters." });
            if (model.NewPassword.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters." });
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                model.NewPassword,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#]).+$"))
                return BadRequest(new { message = "Password must contain uppercase, lowercase, number and special character." });
            if (BCrypt.Net.BCrypt.Verify( model.NewPassword, user.PasswordHash))
            {
                return BadRequest(new
                { message = "New password cannot be the same as the current password." });
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        }
        await _context.SaveChangesAsync();
        if (!string.IsNullOrWhiteSpace(oldImage) && oldImage != user.ProfileImage)
        { await _supabaseStorageService.DeleteImageAsync(oldImage); }
        return Ok(new
        { message = "Profile updated successfully." });
    }
    [Authorize]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                { message = "No file selected." });
            }
            var imageUrl = await _supabaseStorageService.UploadImageAsync(file);
            return Ok(new
            { imageUrl, message = "Image uploaded successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {  message = "Failed to upload image.", error = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new
            { message = "User not found." });
        }
        if (!string.IsNullOrWhiteSpace(user.ProfileImage))
        {
            await _supabaseStorageService.DeleteImageAsync(user.ProfileImage);
        }
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new
        { message = "User deleted successfully." });
    }
}