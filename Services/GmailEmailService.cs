using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MimeKit;

namespace Executive_Fuentes.Services
{
    public class GmailEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            string[] scopes = { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend };
            string applicationName = "EF System";

            var basePath = Directory.GetCurrentDirectory();
            var credentialPath = Path.Combine(basePath, "Credentials", "credentials.json");

            using var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read);

            string tokenPath = Path.Combine(basePath, "token.json");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenPath, true)
            );

            var service = new Google.Apis.Gmail.v1.GmailService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("EF System", "yourgmail@gmail.com")); // replace with your Gmail
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = subject;

            emailMessage.Body = new TextPart("html")
            {
                Text = body
            };

            using var memoryStream = new MemoryStream();
            await emailMessage.WriteToAsync(memoryStream);

            var rawMessage = Convert.ToBase64String(memoryStream.ToArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var message = new Google.Apis.Gmail.v1.Data.Message
            {
                Raw = rawMessage
            };

            await service.Users.Messages.Send(message, "me").ExecuteAsync();
        }
    }
}