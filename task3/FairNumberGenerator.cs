using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class FairNumberGenerator
    {
        private CryptoRandomGenerator cryptoGenerator;
        private HmacCalculator hmacCalculator;

        public FairNumberGenerator()
        {
            cryptoGenerator = new CryptoRandomGenerator();
            hmacCalculator = new HmacCalculator();
        }

        public Tuple<int, byte[], string, int> GenerateFairNumber(int maxValue)
        {
            int computerChoice = CryptoRandomGenerator.GenerateSecureRandom(maxValue);
            byte[] secretKey = CryptoRandomGenerator.GenerateSecureKey();
            string hmacValue = HmacCalculator.CalculateHmac(secretKey, computerChoice);
            return Tuple.Create(computerChoice, secretKey, hmacValue, maxValue);
        }
    }
}
