using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VenueBooking.Services
{
    public class RazorpayService
    {
        private readonly HttpClient _httpClient;
        private readonly string _keyId;
        private readonly string _keySecret;

        public RazorpayService(IConfiguration configuration)
        {
            _keyId = configuration["Razorpay:KeyId"]
                     ?? throw new InvalidOperationException("Razorpay:KeyId not configured.");
            _keySecret = configuration["Razorpay:KeySecret"]
                         ?? throw new InvalidOperationException("Razorpay:KeySecret not configured.");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.razorpay.com/v1/")
            };

            // Basic auth: key_id:key_secret
            var authBytes = Encoding.ASCII.GetBytes($"{_keyId}:{_keySecret}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }

        public string KeyId => _keyId;

        /// <summary>
        /// Creates a Razorpay order. Amount is in INR (rupees); Razorpay expects paise.
        /// Returns a dictionary with order details including "id".
        /// </summary>
        public async Task<Dictionary<string, object>?> CreateOrderAsync(decimal amountInRupees, string currency, string receipt)
        {
            var payload = new Dictionary<string, object>
            {
                { "amount", (int)(amountInRupees * 100) }, // Convert to paise
                { "currency", currency },
                { "receipt", receipt },
                { "payment_capture", 1 } // Auto-capture
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("orders", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
        }

        /// <summary>
        /// Verifies the Razorpay payment signature using HMAC-SHA256.
        /// This ensures the payment response is authentic and not tampered with.
        /// </summary>
        public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
        {
            var message = $"{orderId}|{paymentId}";
            var keyBytes = Encoding.UTF8.GetBytes(_keySecret);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var computedHash = hmac.ComputeHash(messageBytes);
            var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();

            return string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }
    }
}