using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MturkSms.Api
{
    public static class BeaconApi
    {
        private static readonly string CALLER_ID = "ZoZr3t91zEudKO4F_jCqYA";
        private static readonly string KEY = "9802e0f2245cf19f950fef5bdc6b6dfd";

        private static readonly string API_ENDPOINT = "https://dev.api.stabilitasventures.net/beacon/1";

        public static async Task<HttpResponseMessage> PostCheckinNotification(string notificationId, string userId)
        {
            return await PostNotification(notificationId, userId, "checkin");
        }

        public static async Task<HttpResponseMessage> PostPanicNotification(string notificationId, string userId)
        {
            return await PostNotification(notificationId, userId, "panic");
        }

        private static async Task<HttpResponseMessage> PostNotification(string notificationId, string userId, string operation)
        {
            var endpoint = $"/notifications/{notificationId}/responses";
            var payload = new
            {
                NotificationId = notificationId,
                UserId = userId,
                Response = operation,
            };

            return await CallApi("POST", endpoint, payload);
        }

        private static async Task<HttpResponseMessage> CallApi(string method, string endpoint, object data = null)
        {
            var time = (new DateTimeOffset(DateTime.UtcNow)).ToUnixTimeSeconds();

            using (var client = new HttpClient())
            {
                // client.BaseAddress = new Uri(API_ENDPOINT);
                client.DefaultRequestHeaders.Add("X-SVBeacon-User", CALLER_ID);
                client.DefaultRequestHeaders.Add("X-SVBeacon-TS", time.ToString());
                client.DefaultRequestHeaders.Add("Authorization", $"SVAuth1 {CalcSignature(method, CALLER_ID, new Uri($"{API_ENDPOINT}{endpoint}"), time)}");
                client.DefaultRequestHeaders.Add("User-Agent", ".NET Core HttpClient");

                var payload = String.Empty;
                if (data != null)
                {
                    payload = BeaconJsonConvert.SerializeObject(data);
                }

                // should convert this mess to HttpRequestMessage...
                method = method.ToUpperInvariant();
                if (method == "GET")
                {
                    return await client.GetAsync(endpoint);
                }
                else if (method == "POST")
                {
                    Console.WriteLine($"POSTing to API endpoint {endpoint}");
                    return await client.PostAsync($"{API_ENDPOINT}{endpoint}", new StringContent(payload, Encoding.UTF8, "application/json"));
                }

                return null;
            }
        }

        public static string CalcSignature(string method, string uid, Uri url, Int64 time)
        {
            var signingString = method.ToUpperInvariant() + "\n"
                                + url.AbsolutePath + "\n"
                                + BeaconUtils.UriEscape(uid) + "\n"
                                + time;

            // var hmac = new HMACSHA256(ToHexBytes(KEY));
            var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(KEY));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingString))).Replace("=", "");

            return signature;
        }

        private static byte[] ToHexBytes(string hex)
        {
            if (hex == null) return null;
            if (hex.Length == 0) return new byte[0];

            int l = hex.Length / 2;
            var b = new byte[l];
            for (int i = 0; i < l; ++i)
            {
                b[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return b;
        }
    }
}