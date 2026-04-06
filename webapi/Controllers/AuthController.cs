using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using webapi.Data;
using webapi.Models;
using webapi.Models.DTOs;
using webapi.Models.ModelsImpl;
using webapi.Services;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<AuthController> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var token = await GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return Unauthorized("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!result.Succeeded)
        {
            return Unauthorized("Invalid email or password");
        }

        var token = await GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        // Get user ID from the current token claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Invalid token");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Unauthorized("User not found");
        }

        // Generate a new token
        var token = await GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    [HttpPost("magic-link/request")]
    public async Task<IActionResult> RequestMagicLink([FromBody] MagicLinkRequestDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            // Don't reveal if user exists or not
            _logger.LogWarning("Magic link requested for non-existent email: {Email}", model.Email);
            return Ok(new { Message = "If the email exists, a magic link has been sent." });
        }

        // Generate a unique token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Create magic link record
        var magicLink = new MagicLink
        {
            Id = Guid.NewGuid(),
            Token = token,
            Email = model.Email,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.MagicLinks.Add(magicLink);
        await _context.SaveChangesAsync();

        // Send email with magic link
        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5000";
        var magicLinkUrl = $"{frontendUrl}/auth/verify-magic-link?token={token}";

        try
        {
            await _emailService.SendMagicLinkAsync(user.Email!, magicLinkUrl);
            _logger.LogInformation("Magic link sent to {Email}", model.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send magic link email to {Email}", model.Email);
            return StatusCode(500, "Failed to send magic link email");
        }

        return Ok(new { Message = "If the email exists, a magic link has been sent." });
    }

    [HttpPost("magic-link/verify")]
    public async Task<IActionResult> VerifyMagicLink([FromBody] MagicLinkVerifyDto model)
    {
        // Find the magic link in the database
        var magicLink = await _context.MagicLinks
            .FirstOrDefaultAsync(ml => ml.Token == model.Token);

        if (magicLink == null)
        {
            return Unauthorized("Invalid or expired magic link");
        }

        // Check if already used
        if (magicLink.IsUsed)
        {
            return Unauthorized("This magic link has already been used");
        }

        // Check if expired
        if (magicLink.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Unauthorized("This magic link has expired");
        }

        // Find the user
        var user = await _userManager.FindByEmailAsync(magicLink.Email);

        if (user == null)
        {
            return Unauthorized("User not found");
        }

        // Mark the magic link as used
        magicLink.IsUsed = true;
        magicLink.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = await GenerateJwtToken(user);

        _logger.LogInformation("User {Email} authenticated via magic link", user.Email);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add each role as a separate ClaimTypes.Role claim
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
