namespace webapi.Models.ModelsImpl;

/// <summary>
/// Represents a magic link token for passwordless authentication
/// </summary>
public class MagicLink : IModel
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The unique token for the magic link
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// The email address this magic link is for
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// When this magic link expires
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Whether this magic link has been used
    /// </summary>
    public bool IsUsed { get; set; }
}
