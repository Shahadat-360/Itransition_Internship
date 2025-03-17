using task3;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            if (args.Length < 3)
            {
                throw new ArgumentException($"At least 3 dice configurations required, got {args.Length}");
            }
            List<Dice> diceList = DiceParser.parse_dice_args(args.ToList());
            TableGenerator.GenerateProbabilityTable(diceList);
            GameController game = new GameController(diceList);
            game.PlayGame();
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Example usage: dotnet run 2,2,4,4,9,9 6,8,1,1,8,6 7,5,3,7,5,3");
            Environment.Exit(1);
        }
    }
}
