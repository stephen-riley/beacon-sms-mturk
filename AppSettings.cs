using StackExchange.Redis;

namespace MturkSms
{
    public class AppSettings
    {
        public AppSettings()
        {
        }

        public string RedisString { get; set; }

        public ConnectionMultiplexer RedisConnection { get; set; }
    }
}