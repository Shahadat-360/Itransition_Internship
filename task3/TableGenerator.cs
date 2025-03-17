using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class TableGenerator
    {
        public static void GenerateProbabilityTable(List<Dice> diceList)
        {
            int n = diceList.Count;

            var headers = new List<string> { "User dice v" };
            for (int i = 0; i < n; i++)
            {
                headers.Add($"{diceList[i]}");
            }

            var table = new ConsoleTable(headers.ToArray());

            for (int i = 0; i < n; i++)
            {
                var rowData = new List<string>();
                rowData.Add($"{diceList[i]}");

                for (int j = 0; j < n; j++)
                {
                    double probability;
                    if (i == j)
                    {
                        probability = 0.5;
                    }
                    else
                    {
                        probability = ProbabilityCalculator.CalculateWinProbability(diceList[i], diceList[j]);
                    }

                    rowData.Add(i == j ? $"-({probability:F2})" : $"{probability:F2}");
                }

                table.AddRow(rowData.ToArray());
            }
            Console.WriteLine("Probability of the win for the user:");
            table.Write();
        }
    }
}
