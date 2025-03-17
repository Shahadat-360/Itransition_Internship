using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class HmacCalculator
    {
        public static string CalculateHmac(byte[] key, int message)
        {
            using (var hmac = new HMACSHA3_256(key))
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message.ToString());
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }
    }
}
