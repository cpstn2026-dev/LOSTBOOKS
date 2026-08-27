using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LOSTBOOKS.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            string host = _config["EmailSettings:SmtpHost"]!;
            int port = int.Parse(_config["EmailSettings:SmtpPort"]!);
            string senderEmail = _config["EmailSettings:SenderEmail"]!;
            string senderName = _config["EmailSettings:SenderName"]!;
            string appPassword = _config["EmailSettings:AppPassword"]!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}