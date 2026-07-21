using System.Net.Http;

namespace SchoolFeesSystem.Services
{
    public class SmsService
    {
        private readonly string _apiKey = "ZHVtRVVvYnJBb2F1UVRlbHJRaEU"; // Get this from Arkesel Dashboard
        private readonly string _senderId = "SCH_FEES"; // Your approved 11-character Sender ID

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            using (var client = new HttpClient())
            {
                // Format phone number to 233 format
                if (phoneNumber.StartsWith("0")) phoneNumber = "233" + phoneNumber.Substring(1);

                var url = $"https://sms.arkesel.com/sms/api?action=send-sms&api_key={_apiKey}&to={phoneNumber}&from={_senderId}&sms={Uri.EscapeDataString(message)}";

                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
        }
    }
}