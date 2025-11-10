using System.Collections.ObjectModel;

namespace GeneralaGame
{
    public static class GameLogic
    {
        public static int CalculateSum(List<int> dice, int valueToCount)
        {
            return dice.Where(d => d == valueToCount).Sum();
        }

        //  Straight
        public static int CalculateStraight(List<int> dice, bool isFirstRoll)
        {
            var uniqueDice = dice.Distinct().OrderBy(d => d).ToList();

            bool isSmallStraight = uniqueDice.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 });
            bool isBigStraight = uniqueDice.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });

            if (isSmallStraight || isBigStraight)
            {
                // 20 очок + 5 бонусних, якщо це перший кидок
                return isFirstRoll ? 25 : 20;
            }
            return 0;
        }

        // 3. Full House
        public static int CalculateFullHouse(List<int> dice, bool isFirstRoll)
        {
            var groups = GetDiceCounts(dice);

            // Full House - це коли є група з 3 і група з 2
            bool hasThreeOfAKind = groups.Any(g => g.Count == 3);
            bool hasTwoOfAKind = groups.Any(g => g.Count == 2);

            if (hasThreeOfAKind && hasTwoOfAKind)
            {
                // 30 очок + 5 бонусних
                return isFirstRoll ? 35 : 30;
            }
            return 0;
        }

        // 4. Four of a Kind
        public static int CalculateFourOfAKind(List<int> dice, bool isFirstRoll)
        {
            var groups = GetDiceCounts(dice);

            // Чи є група з 4 або 5 (Generala - це теж "Four of a Kind")
            if (groups.Any(g => g.Count >= 4))
            {
                // 40 очок + 5 бонусних
                return isFirstRoll ? 45 : 40;
            }
            return 0;
        }

        // 5. Generala
        public static int CalculateGenerala(List<int> dice, bool isFirstRoll)
        {
            var groups = GetDiceCounts(dice);

            if (groups.Any(g => g.Count == 5))
            {
                // 50 очок (бонус обробляється окремо як "миттєва перемога")
                return 50;
            }
            return 0;
        }

        // --- ДОПОМІЖНИЙ МЕТОД ---

        // Цей метод перевіряє, чи є 5 однакових кубиків
        // (потрібен для миттєвої перемоги)
        public static bool CheckForInstantWin(List<int> dice)
        {
            return GetDiceCounts(dice).Any(g => g.Count == 5);
        }

        // Приватний метод, що групує кубики
        // Наприклад: [1, 1, 3, 3, 3] -> [ (1, 2), (3, 3) ]
        private static List<(int Value, int Count)> GetDiceCounts(List<int> dice)
        {
            return dice.GroupBy(d => d)
                       .Select(g => (Value: g.Key, Count: g.Count()))
                       .ToList();
        }

        private const int MONTE_CARLO_ITERATIONS = 2000;

        private const int MONTE_CARLO_ROLL_ITERATIONS = 500;

        // Окремий Random для симуляцій, щоб не впливати на кидки у UI
        private static Random _simRandom = new Random();

        public static (List<int> diceToHold, double expectedValue) FindBestDiceToHold_MonteCarlo(
             List<int> currentDice,
             ObservableCollection<ScoreEntry> currentBoard,
             int currentRound,
             int rollsLeft)
        {
            List<int> bestHoldCombination = new List<int>();
            double bestAverageScore = -1.0;

            for (int i = 0; i < 32; i++)
            {
                List<int> diceToHold = new List<int>();
                List<int> diceToRoll = new List<int>();

                for (int j = 0; j < 5; j++)
                {
                    if (((i >> j) & 1) == 1)
                    {
                        diceToHold.Add(currentDice[j]);
                    }
                    else
                    {
                        diceToRoll.Add(currentDice[j]);
                    }
                }

                double totalSimScore = 0;
                int diceToRollCount = 5 - diceToHold.Count;

                for (int k = 0; k < MONTE_CARLO_ROLL_ITERATIONS; k++)
                {
                    List<int> finalSimDice = new List<int>(diceToHold);
                    for (int r = 0; r < diceToRollCount; r++)
                    {
                        finalSimDice.Add(_simRandom.Next(1, 7));
                    }

                    // --- ▼▼▼ ЗМІНА ▼▼▼ ---
                    // "Жадібна" логіка винесена у новий метод
                    totalSimScore += FindBestGreedyScore(finalSimDice, currentBoard, false);
                    // --- ▲▲▲ КІНЕЦЬ ЗМІНИ ▲▲▲ ---
                }

                double averageScore = totalSimScore / MONTE_CARLO_ROLL_ITERATIONS;

                if (averageScore > bestAverageScore)
                {
                    bestAverageScore = averageScore;
                    bestHoldCombination = diceToHold;
                }
            }

            return (bestHoldCombination, bestAverageScore);
        }

        public static (ScoreEntry bestEntry, double expectedValue) FindBestCategory_MonteCarlo(
            List<int> finalDice,
            ObservableCollection<ScoreEntry> currentBoard,
            int currentRound,
            bool isFirstRoll)
        {
            ScoreEntry bestEntry = null;
            double bestAverageScore = -1.0;

            var availableEntries = currentBoard.Where(e => !e.ComputerFinalScore.HasValue
                                                    && e.CategoryName != "Total Score");

            // --- ОНОВЛЕНО (обробка, якщо ходів немає) ---
            if (!availableEntries.Any())
            {
                // Повертаємо "немає ходу" і "найгірший рахунок"
                return (null, bestAverageScore);
            }

            foreach (var entry in availableEntries)
            {
                int potentialScore = CalculateScoreForEntry(entry.CategoryName, finalDice, isFirstRoll);
                double totalSimScore = 0;

                for (int i = 0; i < MONTE_CARLO_ITERATIONS; i++)
                {
                    ObservableCollection<ScoreEntry> boardCopy = new ObservableCollection<ScoreEntry>();
                    foreach (var se in currentBoard) boardCopy.Add(se.Clone());

                    boardCopy.First(e => e.CategoryName == entry.CategoryName).ComputerFinalScore = potentialScore;

                    totalSimScore += SimulateGameToEnd(boardCopy, currentRound + 1);
                }

                double averageScore = totalSimScore / MONTE_CARLO_ITERATIONS;

                if (averageScore > bestAverageScore)
                {
                    bestAverageScore = averageScore;
                    bestEntry = entry;
                }
            }

            // --- ОНОВЛЕНО (повертаємо і комірку, і її цінність) ---
            return (bestEntry, bestAverageScore);
        }

        private static int SimulateGameToEnd(ObservableCollection<ScoreEntry> board, int startRound)
        {
            for (int round = startRound; round <= 10; round++)
            {
                // 1. Кидаємо кубики
                List<int> simDice = new List<int>();
                for (int i = 0; i < 5; i++) simDice.Add(_simRandom.Next(1, 7));

                // 2. "Жадібно" обираємо найкращий хід з вільних
                var available = board.Where(e => !e.ComputerFinalScore.HasValue && e.CategoryName != "Total Score");
                if (!available.Any()) break;

                // --- ▼▼▼ ЗМІНА ▼▼▼ ---
                // Використовуємо новий хелпер
                int maxScore = FindBestGreedyScore(simDice, board, true);

                // Знаходимо комірку, яка дає цей рахунок
                // (це не ідеально, але для симуляції достатньо)
                var bestSimEntry = available.FirstOrDefault(e => CalculateScoreForEntry(e.CategoryName, simDice, true) == maxScore);
                if (bestSimEntry == null) // Якщо всі дають 0, беремо першу вільну
                    bestSimEntry = available.First();
                // --- ▲▲▲ КІНЕЦЬ ЗМІНИ ▲▲▲ ---

                // 3. Записуємо його
                bestSimEntry.ComputerFinalScore = maxScore;
            }

            // 4. Повертаємо фінальний "Total Score"
            int total = board.Where(e => e.CategoryName != "Total Score").Sum(e => e.ComputerFinalScore ?? 0);
            return total;
        }

        private static int FindBestGreedyScore(List<int> dice, ObservableCollection<ScoreEntry> board, bool isFirstRoll)
        {
            int maxPossibleScore = 0;
            var available = board.Where(e => !e.ComputerFinalScore.HasValue && e.CategoryName != "Total Score");
            foreach (var entry in available)
            {
                int score = CalculateScoreForEntry(entry.CategoryName, dice, isFirstRoll);
                if (score > maxPossibleScore)
                {
                    maxPossibleScore = score;
                }
            }
            return maxPossibleScore;
        }

        public static int CalculateScoreForEntry(string categoryName, List<int> dice, bool isFirstRoll)
        {
            switch (categoryName)
            {
                case "Ones": return CalculateSum(dice, 1);
                case "Twos": return CalculateSum(dice, 2);
                case "Threes": return CalculateSum(dice, 3);
                case "Fours": return CalculateSum(dice, 4);
                case "Fives": return CalculateSum(dice, 5);
                case "Sixes": return CalculateSum(dice, 6);
                case "Straight": return CalculateStraight(dice, isFirstRoll);
                case "Full House": return CalculateFullHouse(dice, isFirstRoll);
                case "Four of a kind": return CalculateFourOfAKind(dice, isFirstRoll);
                case "Generala": return CalculateGenerala(dice, isFirstRoll);
                default: return 0;
            }
        }
    }
}