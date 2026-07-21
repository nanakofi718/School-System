using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SchoolFeesSystem.Services
{
    public class NotificationService
    {
        private const string accountSid = "YOUR_TWILIO_SID";
        private const string authToken = "YOUR_TWILIO_TOKEN";
        private const string fromPhone = "+1234567890"; // Your Twilio Number

        public async Task SendPaymentAlert(string toPhone, string studentName, decimal amount, decimal balance)
        {
            TwilioClient.Init(accountSid, authToken);

            var messageBody = $"Payment Received: GHS {amount} for {studentName}. Remaining balance: GHS {balance}. Thank you!";

            // 1. Send SMS
            await MessageResource.CreateAsync(
                body: messageBody,
                from: new PhoneNumber(fromPhone),
                to: new PhoneNumber(toPhone)
            );

            // 2. Send WhatsApp (Twilio requires "whatsapp:" prefix)
            await MessageResource.CreateAsync(
                from: new PhoneNumber($"whatsapp:{fromPhone}"),
                to: new PhoneNumber($"whatsapp:{toPhone}"),
                body: messageBody
            );
        }
    }
}