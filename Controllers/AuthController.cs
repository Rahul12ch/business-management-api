
using client.Data;
using client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

    public AuthController( AppDbContext context, IConfiguration configuration)
    
    {  _context = context; _configuration = configuration; }
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users .Select(x => new
            {  x.Id, x.Username,  x.Email })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        if (await _context.Users.AnyAsync(x => x.Username == user.Username))
        { return BadRequest("Username already exists"); }

        if (await _context.Users.AnyAsync(x => x.Email == user.Email))
        { return BadRequest("Email already exists"); }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.CreatedDate = DateTime.Now; _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "User registered successfully",
            username = user.Username,
            email = user.Email,
            userId = user.Id
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(User model)
    {
        var user = await _context.Users .FirstOrDefaultAsync(x => x.Email == model.Email);
        if (user == null)  return Unauthorized("Invalid Email");

        bool valid = BCrypt.Net.BCrypt.Verify(
         model.PasswordHash, user.PasswordHash);

        if (!valid) return Unauthorized("Invalid Password");

        var claims = new[]
        {
            new Claim( ClaimTypes.Name, user.Username),
            new Claim( ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        var key = new SymmetricSecurityKey(  Encoding.UTF8.GetBytes( _configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(  key,  SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(  issuer: _configuration["Jwt:Issuer"],
         
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds);
        return Ok(new
        {
            token = new JwtSecurityTokenHandler() .WriteToken(token),
            username = user.Username, email = user.Email, userId = user.Id
        });
    }
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst( ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(); }
        var user = await _context.Users .FirstOrDefaultAsync(x => x.Id == int.Parse(userId));
        if (user == null)  return NotFound();
        return Ok(new
        {
         user.Id, user.Username,user.Email,user.CreatedDate, user.ProfileImage });
    }
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
    UpdateProfileModel model)
    {
        var userId = User.FindFirst( ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        { 
            return Unauthorized();}
       var user = await _context.Users .FirstOrDefaultAsync(x => x.Id == int.Parse(userId));
        if (user == null) return NotFound();
        if (await _context.Users.AnyAsync(x =>  x.Username == model.Username &&  x.Id != user.Id))
        {
            return BadRequest( "Username already exists");
        }
        if (await _context.Users.AnyAsync(x => x.Email == model.Email && x.Id != user.Id))
        {
            return BadRequest( "Email already exists"); }
        user.Username = model.Username.Trim(); user.Email = model.Email.Trim(); user.ProfileImage = model.ProfileImage;

        if (!string.IsNullOrWhiteSpace( model.NewPassword))
        {
            var valid = BCrypt.Net.BCrypt.Verify(
             model.CurrentPassword, user.PasswordHash);
            if (!valid)
            {
                return BadRequest( "Current password is incorrect");
            }
            user.PasswordHash =  BCrypt.Net.BCrypt.HashPassword( model.NewPassword); }
        await _context.SaveChangesAsync();
        return Ok(new
        { message = "Profile updated successfully" });
    }
    [Authorize]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage( IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file selected");
        }
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine(
        Directory.GetCurrentDirectory(),"wwwroot","uploads");
        if (!Directory.Exists( uploadsFolder))
        {
         Directory.CreateDirectory(uploadsFolder);
        }
        var filePath =Path.Combine(uploadsFolder, fileName);
        using (var stream = new FileStream( filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        return Ok(new
        {
            imageUrl = $"uploads/{fileName}" });
    }
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users .FindAsync(id);
        if (user == null)return NotFound(); _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new
        { message = "User deleted successfully"});
    }
}