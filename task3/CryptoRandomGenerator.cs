using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class CryptoRandomGenerator
    {
        public static byte[] GenerateSecureKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] key = new byte[32];
                rng.GetBytes(key);
                return key;
            }
        }

        public static int GenerateSecureRandom(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentException("Max value must be non-negative");
            }

            if (maxValue == 0)
            {
                return 0;
            }

            int byteCount = (int)Math.Ceiling(Math.Log2(maxValue + 1) / 8);
            if (byteCount == 0)
            {
                byteCount = 1;
            }

            using (var rng = RandomNumberGenerator.Create())
            {
                while (true)
                {
                    byte[] randomBytes = new byte[byteCount];
                    rng.GetBytes(randomBytes);
                    int value = 0;
                    for (int i = 0; i < byteCount; i++)
                    {
                        value = (value << 8) | randomBytes[i];
                    }

                    if (value <= maxValue)
                    {
                        return value;
                    }
                }
            }
        }
    }
}
