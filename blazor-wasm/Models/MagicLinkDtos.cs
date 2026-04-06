namespace blazor_wasm.Models;

public class MagicLinkRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class MagicLinkVerifyDto
{
    public string Token { get; set; } = string.Empty;
}
