using System.ComponentModel.DataAnnotations;

namespace webapi.Models.DTOs;

/// <summary>
/// DTO for requesting a magic link
/// </summary>
public class MagicLinkRequestDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}

/// <summary>
/// DTO for verifying a magic link token
/// </summary>
public class MagicLinkVerifyDto
{
    [Required]
    public required string Token { get; set; }
}