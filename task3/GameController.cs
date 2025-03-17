using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task3
{
    class GameController
    {
        private List<Dice> diceList;
        private FairNumberGenerator fairGenerator;
        private TableGenerator tableGenerator;

        public GameController(List<Dice> diceList)
        {
            this.diceList = diceList;
            fairGenerator = new FairNumberGenerator();
            tableGenerator = new TableGenerator();
        }

        public void DisplayMenu(string title, List<string> options, bool allowHelp = true)
        {
            Console.Write(title ?? "");
            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"{i} - {options[i]}");
            }
            Console.WriteLine("X - exit");
            if (allowHelp)
            {
                Console.WriteLine("? - help");
            }
        }

        public int? GetUserSelection(int maxValue, bool allowHelp = true, Action helpCallback = null)
        {
            while (true)
            {
                Console.Write("Enter your selection:");
                string selection = Console.ReadLine().Trim().ToUpper();

                if (selection == "X")
                {
                    Console.WriteLine("Exiting the game.");
                    Environment.Exit(0);
                }

                if (selection == "?" && allowHelp && helpCallback != null)
                {
                    helpCallback();
                    continue;
                }

                if (int.TryParse(selection, out int value) && value >= 0 && value <= maxValue)
                {
                    return value;
                }
                else
                {
                    Console.WriteLine($"Invalid input. Please enter a number between 0 and {maxValue}, X to exit" +
                                      (allowHelp ? ", or ? for help." : "."));
                }
            }
        }

        public bool DetermineFirstMove()
        {
            Console.WriteLine("Let's determine who makes the first move.");
            var (computerChoice, secretKey, hmacValue, maxValue) = fairGenerator.GenerateFairNumber(1);
            Console.WriteLine($"I selected a random value in the range 0..1 (HMAC={hmacValue}).");
            Console.WriteLine("Try to guess my selection.");

            DisplayMenu("", new List<string> { "0", "1" }, false);
            int? userChoice = GetUserSelection(1, false);

            int result = (computerChoice + userChoice.Value) % 2;
            string keyHex = BitConverter.ToString(secretKey).Replace("-", "").ToUpper();
            Console.WriteLine($"My selection: {computerChoice} (KEY={keyHex}).");

            if (result == 0)
            {
                Console.WriteLine("You make the first move.");
                return false;
            }
            else
            {
                Console.WriteLine("I make the first move.");
                return true;
            }
        }

        public int SelectDice(List<int> availableIndices, bool isComputer)
        {
            List<Dice> availableDice = availableIndices.Select(i => diceList[i]).ToList();

            if (isComputer)
            {
                int selectedIndex = availableIndices[new Random().Next(availableIndices.Count)];
                Dice selectedDice = diceList[selectedIndex];
                Console.WriteLine($"I choose the [{selectedDice}] dice.");
                return selectedIndex;
            }
            else
            {
                List<string> options = availableDice.Select(d => d.ToString()).ToList();

                void ShowHelp()
                {
                    Console.WriteLine("\nWin probability table (how likely row dice beats column dice):");
                    TableGenerator.GenerateProbabilityTable(diceList);
                    Console.WriteLine();
                }

                DisplayMenu("Choose your dice:\n", options, true);
                int? userSelection = GetUserSelection(options.Count - 1, true, ShowHelp);
                int selectedIndex = availableIndices[userSelection.Value];
                Dice selectedDice = diceList[selectedIndex];
                Console.WriteLine($"You choose the [{selectedDice}] dice.");
                return selectedIndex;
            }
        }

        public int PerformRoll(int diceIndex, bool isComputer)
        {
            Dice dice = diceList[diceIndex];
            int faceCount = dice.get_face_count();
            int maxIndex = faceCount - 1;

            Console.WriteLine($"It's time for {(isComputer ? "my" : "your")} roll.");

            var (computerChoice, secretKey, hmacValue, _) = fairGenerator.GenerateFairNumber(maxIndex);

            Console.WriteLine($"I selected a random value in the range 0..{maxIndex} (HMAC={hmacValue}).");
            Console.WriteLine($"Add your number modulo {faceCount}.");

            List<string> options = Enumerable.Range(0, faceCount).Select(i => i.ToString()).ToList();
            DisplayMenu("", options, true);

            void ShowRollHelp()
            {
                Console.WriteLine("\nSelect a number to add to the computer's hidden number.");
                Console.WriteLine("The result will be calculated as (computer_number + your_number) modulo number_of_faces.");
                Console.WriteLine("This ensures a fair random generation that neither party can manipulate.");
                Console.WriteLine();
            }

            int? userChoice = GetUserSelection(maxIndex, true, ShowRollHelp);

            int resultIndex = (computerChoice + userChoice.Value) % faceCount;
            string keyHex = BitConverter.ToString(secretKey).Replace("-", "").ToUpper();
            Console.WriteLine($"My number is {computerChoice} (KEY={keyHex}).");
            Console.WriteLine($"The fair number generation result is {computerChoice} + {userChoice.Value} = {resultIndex} (mod {faceCount}).");

            int faceValue = dice.get_face(resultIndex);
            Console.WriteLine($"{(isComputer ? "My" : "Your")} roll result is {faceValue}.");

            return faceValue;
        }

        public void PlayGame()
        {
            bool computerFirst = DetermineFirstMove();
            List<int> availableIndices = Enumerable.Range(0, diceList.Count).ToList();

            int computerDiceIndex, userDiceIndex;
            if (computerFirst)
            {
                computerDiceIndex = SelectDice(availableIndices, true);
                availableIndices.Remove(computerDiceIndex);
                userDiceIndex = SelectDice(availableIndices, false);
            }
            else
            {
                userDiceIndex = SelectDice(availableIndices, false);
                availableIndices.Remove(userDiceIndex);
                computerDiceIndex = SelectDice(availableIndices, true);
            }

            int computerResult = PerformRoll(computerDiceIndex, true);
            int userResult = PerformRoll(userDiceIndex, false);

            if (userResult > computerResult)
            {
                Console.WriteLine($"You win ({userResult} > {computerResult})!");
            }
            else if (computerResult > userResult)
            {
                Console.WriteLine($"I win ({computerResult} > {userResult})!");
            }
            else
            {
                Console.WriteLine($"It's a tie ({userResult} = {computerResult})!");
            }
        }
    }
}
