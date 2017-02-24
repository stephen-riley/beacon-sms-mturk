using System;
using System.Text;

namespace MturkSms.Api
{
    public static class BeaconUtils
    {
        public static string UriEscape(string component)
        {
            var esc = Uri.EscapeUriString(component);
            esc = esc.Replace("+", "%2B");
            return esc;
        }

        public static string Md5Hash(string content)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(content);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}