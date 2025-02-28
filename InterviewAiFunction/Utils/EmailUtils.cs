using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SendGrid;
using SendGrid.Helpers.Mail;
using RestSharp; // RestSharp v112.1.0
using RestSharp.Authenticators;


namespace InterviewAiFunction.Utils
{
    internal class EmailUtils
    {

        public EmailUtils() { }
       
        public static async Task ExecuteSendGrid(string toEmail, string toName, string fromName, string url)
        {
            var apiKey = Environment.GetEnvironmentVariable("MAIL_API_KEY");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("idbtechlab@gmail.com", "TechLab");
            var to = new EmailAddress(toEmail, toName);

            var templateId = "d-c6154222e301443f9480769f6cd89989";
            var dynamicTemplateData = new
            {
                user = fromName,
                link = url,
            };
            var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, dynamicTemplateData);

            var response = await client.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Email has been sent successfully");
            }
            else
            {
                Console.WriteLine("Error sending the email: " + response.ToString());
            }
        }

        public static async Task ExecuteMailgun(string toEmail, string toName, string fromName, string url)
        {
            var options = new RestClientOptions("https://api.mailgun.net/v3")
            {
                Authenticator = new HttpBasicAuthenticator("api", Environment.GetEnvironmentVariable("MAIL_API_KEY") ?? "MAIL_API_KEY")
            };

            var client = new RestClient(options);
            var request = new RestRequest("/mail.sndbx.run/messages", Method.Post);
            request.AlwaysMultipartFormData = true;
            request.AddParameter("from", "TechLab Sandbox <postmaster@mail.sndbx.run>");
            request.AddParameter("to", toEmail);
            request.AddParameter("subject", "You have an Interview GPT Invitation");
            request.AddParameter("template", "interview gpt template");
            //request.AddParameter("h:X-Mailgun-Variables", "{\"url\": \"test\"}");
            request.AddParameter("h:X-Mailgun-Variables", $"{{\"url\": \"{url}\", \"user\":\"{toName}\"}}");
            var response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Email has been sent successfully");
            }
            else
            {

                Console.WriteLine("Error sending the email: " + response.Content);
            }
        }
    }
}
