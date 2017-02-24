using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MturkSms.Api;
using Newtonsoft.Json;
using Nexmo.Api;
using StackExchange.Redis;

namespace MturkSms.Controllers
{
    public class SmsController : Controller
    {
        private class Filter
        {
            [JsonProperty("c")]
            public string Country { get; set; }

            [JsonProperty("m")]
            public string Carrier { get; set; }
        }

        private readonly AppSettings appSettings;

        public SmsController(IOptions<AppSettings> settings)
        {
            appSettings = settings.Value;
            if (appSettings.RedisConnection == null || appSettings.RedisConnection.IsConnected == false)
            {
                Console.WriteLine($"Opening Redis connection to {appSettings.RedisString}");
                appSettings.RedisConnection = ConnectionMultiplexer.Connect(appSettings.RedisString);
            }
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Process([FromQuery]string num, [FromQuery]string token)
        {
            var cleanNum = Regex.Replace(num ?? "", "[^0-9]", "");
            token = token ?? "e30=";
            var filter = BeaconJsonConvert.DeserializeObject<Filter>(Encoding.ASCII.GetString(Convert.FromBase64String(token)));

            // Get phone number info
            var numInfo = NumberInsight.RequestStandard(new NumberInsight.NumberInsightBasicRequest { number = cleanNum });

            if (numInfo.status != "0")
            {
                return new ObjectResult(new { Status = "err", Message = "Invalid phone number" }) { StatusCode = 400 };
            }

            var passesFilter = true;
            // Filter on Country if applicable
            if (String.IsNullOrWhiteSpace(filter.Country) == false && filter.Country != "*")
            {
                passesFilter = passesFilter && numInfo.country_code == filter.Country;
            }

            // Filter on Carrier if applicable
            if (String.IsNullOrWhiteSpace(filter.Carrier) == false && filter.Carrier != "*")
            {
                passesFilter = passesFilter && numInfo.current_carrier.network_code == filter.Carrier;
            }

            if (passesFilter)
            {
                var response = await SendMessage(cleanNum, calculateSignature(cleanNum));

                return new ObjectResult(new
                {
                    Status = "ok",
                    NumberResponse = numInfo,
                    MessageResponse = response
                });
            }
            else
            {
                return new ObjectResult(new
                {
                    Status = "err",
                    Message = "number failed test parameters"
                })
                { StatusCode = 400 };
            }
        }

        public IActionResult SubmitCode([FromQuery]string code, [FromQuery]string num)
        {
            var cleanNum = Regex.Replace(num ?? "", "[^0-9]", "");

            if (code.ToLowerInvariant() == calculateSignature(cleanNum).ToLowerInvariant())
            {
                return new ObjectResult(new
                {
                    Status = "ok"
                });
            }
            else
            {
                return new ObjectResult(new
                {
                    Status = "err",
                    Message = "invalid code"
                });
            }
        }

        public IActionResult CheckResponse([FromQuery]string code)
        {
            var redis = appSettings.RedisConnection.GetDatabase();
            var received = redis.StringGet($"TEST:{code}").HasValue;

            return new ObjectResult(new
            {
                Status = received
            });
        }

        private async Task<IActionResult> SendMessage(string num, string code)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://mn.stabilitasventures.net");
                var payload = BeaconJsonConvert.SerializeObject(new
                {
                    To = num,
                    Msg = calculateSignature(num).ToLowerInvariant(),
                    Intent = "two-way",
                    Nid = $"TEST:{code.ToLowerInvariant()}",
                    Uid = $"TEST:{code.ToLowerInvariant()}",
                });
                var response = await client.PostAsync("/router/sms", new StringContent(payload, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return new ObjectResult(response);
                }
                else
                {
                    return new ObjectResult(response) { StatusCode = 400 };
                }
            }
        }

        private string calculateSignature(string num)
        {
            var sum = num.ToCharArray().Select(c => c - 32).Sum();
            var hash = calculateMd5(num);
            return $"{hash.Substring(0, 3)}-{sum}";
        }

        private String calculateMd5(string num)
        {
            string data = "test";
            string hash;
            using (MD5 md5 = MD5.Create())
            {
                md5.Initialize();
                md5.ComputeHash(Encoding.UTF8.GetBytes(data));
                hash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(num)));
                return hash;
            }
        }
    }
}
