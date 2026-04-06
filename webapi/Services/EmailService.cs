namespace webapi.Services;

public interface IEmailService
{
    Task SendMagicLinkAsync(string toEmail, string magicLink);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendMagicLinkAsync(string toEmail, string magicLink)
    {
        var smtpHost = _configuration["SMTP:Host"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["SMTP:Port"] ?? "1025");
        var fromEmail = _configuration["SMTP:FromEmail"] ?? "noreply@localhost";
        var fromName = _configuration["SMTP:FromName"] ?? "Application";

        try
        {
            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = false; // MailHog doesn't use SSL

            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail, fromName),
                Subject = "Your Magic Link",
                Body = $@"
                    <html>
                        <body>
                            <h2>Login to Recht und Ordnung SH</h2>
                            <p>Click the link below to sign in:</p>
                            <p><a href=""{magicLink}"">Sign In</a></p>
                            <p>This link will expire in 15 minutes.</p>
                            <p>If you didn't request this link, you can safely ignore this email.</p>
                        </body>
                    </html>",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Magic link sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send magic link to {Email}", toEmail);
            throw;
        }
    }
}
