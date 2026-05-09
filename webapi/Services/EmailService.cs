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

        var smtpUsername = _configuration["SMTP:Username"];
        var smtpPassword = _configuration["SMTP:Password"];
        var enableSsl = bool.Parse(_configuration["SMTP:EnableSsl"] ?? "false");

        try
        {
            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = enableSsl;

            if (!string.IsNullOrWhiteSpace(smtpUsername))
                client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);

            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail, fromName),
                Subject = "Anmeldelink",
                Body = $@"
                    <html>
                        <body>
                            <h2>Anmeldung bei Recht und Ordnung - Schaffhausen</h2>
                            <p>Klicken Sie auf den untenstehenden Link, um sich anzumelden:</p>
                            <p><a href=""{magicLink}"">Anmelden</a></p>
                            <p>Dieser Link ist 15 Minuten gültig.</p>
                            <p>Falls Sie diesen Link nicht angefordert haben, können Sie diese E-Mail ignorieren.</p>
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
