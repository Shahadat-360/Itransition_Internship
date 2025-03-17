using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class ProbabilityCalculator
    {
        public static double CalculateWinProbability(Dice dice1, Dice dice2)
        {
            int wins = 0;
            int totalComparisons = 0;
            wins = dice1.faces.SelectMany(face1 => dice2.faces.Select(face2 => face1 > face2)).Count(result => result);
            totalComparisons = dice1.faces.Count() * dice2.faces.Count();

            return (double)wins / totalComparisons;
        }
    }
}
