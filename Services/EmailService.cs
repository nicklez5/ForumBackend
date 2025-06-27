using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
namespace MyApi.Services;
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class SendGridEmailService(IConfiguration config) : IEmailService
{
    private readonly IConfiguration _config = config;

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: body, htmlContent: body);
        await client.SendEmailAsync(msg);
    }
}