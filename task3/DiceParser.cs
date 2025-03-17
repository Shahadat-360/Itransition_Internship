using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class DiceParser
    {
        public static List<Dice> parse_dice_args(List<string> args)
        {
            List<Dice> dice = new List<Dice>();
            if (args.Count < 3)
            {
                throw new ArgumentException($"At least 3 dice required, got {args.Count}");
            }
            foreach (string arg in args)
            {
                List<int> face_index = new List<int>();
                try
                {
                    face_index = arg.Split(',').Select(int.Parse).ToList();
                    if (face_index.Count == 0)
                    {
                        throw new ArgumentException($"Empty dice configuration: '{arg}'");
                    }
                    else if (face_index.Count > 6)
                    {
                        throw new ArgumentException($"Invalid Number of Slides in Dice:{arg}.Six Number of slides needed,But Got {face_index.Count}");
                    }
                    dice.Add(new Dice(face_index));
                }
                catch (FormatException)
                {
                    throw new ArgumentException($"All values must be integers: '{arg}'");
                }
                catch (ArgumentException ex)
                {
                    throw;
                }
            }
            return dice;
        }
    }
}
