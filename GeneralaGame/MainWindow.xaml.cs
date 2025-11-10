using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace GeneralaGame
{
    public enum PlayerTurn
    {
        Human,
        Computer
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // --- Колекції ---
        public ObservableCollection<DieModel> Dice { get; set; } = new ObservableCollection<DieModel>();
        public ObservableCollection<ScoreEntry> Scoreboard { get; set; } = new ObservableCollection<ScoreEntry>();

        private Random _random = new Random();

        // --- Властивості стану гри ---
        private int _rollsLeft = 3;
        public int RollsLeft
        {
            get { return _rollsLeft; }
            set
            {
                _rollsLeft = value; OnPropertyChanged();
                OnPropertyChanged(nameof(CanRoll));
            }
        }

        public bool CanRoll
        {
            get
            {
                bool atLeastOneDieIsUnlockable = Dice.Any(die => !die.IsLocked);
                return RollsLeft > 0 && atLeastOneDieIsUnlockable && CurrentTurn == PlayerTurn.Human;
            }
        }

        private PlayerTurn _currentTurn = PlayerTurn.Human;
        public PlayerTurn CurrentTurn
        {
            get { return _currentTurn; }
            set
            {
                _currentTurn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayerIconPath));
                OnPropertyChanged(nameof(CanRoll));
            }
        }

        public string PlayerIconPath => CurrentTurn == PlayerTurn.Human ? "/Images/active_human.png" : "/Images/active_computer.png";

        private int _currentRound = 1;
        public int CurrentRound
        {
            get => _currentRound;
            set
            {
                _currentRound = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoundDisplay));
            }
        }
        public int TotalRounds => 10;
        public string RoundDisplay => $"Round: {CurrentRound} / {TotalRounds}";

        private bool _isNewGameButtonEnabled = false;
        public bool IsNewGameButtonEnabled
        {
            get => _isNewGameButtonEnabled;
            set
            {
                _isNewGameButtonEnabled = value;
                OnPropertyChanged();
            }
        }
        // --- Конструктор ---
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeDice();
            InitializeScoreboard(); // ВИПРАВЛЕНО           
        }

        // --- Методи ініціалізації ---
        private void InitializeDice()
        {
            Dice.Clear();
            for (int i = 0; i < 5; i++)
            {
                Dice.Add(new DieModel { Value = _random.Next(1, 7) });
            }
        }
        private void InitializeScoreboard()
        {
            Scoreboard.Clear(); // На випадок рестарту
            List<string> categories = new List<string>
            {
                "Ones", "Twos", "Threes", "Fours", "Fives", "Sixes",
                "Straight", "Full House", "Four of a kind", "Generala"
            };

            foreach (var category in categories)
            {
                // Ми не встановлюємо рахунки, вони 'null' за замовчуванням
                Scoreboard.Add(new ScoreEntry { CategoryName = category });
            }

            // Додаємо Total Score окремо
            Scoreboard.Add(new ScoreEntry { CategoryName = "Total Score", HumanFinalScore = 0, ComputerFinalScore = 0 });
        }


        // --- Обробники кліків ---
        private async void ScoreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entry = button.DataContext as ScoreEntry;

            if (entry == null || !entry.IsHumanScorable) return;

            // 1. Записуємо фінальний рахунок
            entry.HumanFinalScore = entry.HumanPotentialScore;

            // 2. Оновлюємо загальний рахунок
            UpdateTotalScore();

            // 3. Завершуємо хід
            await EndTurn();
        }

        private async void RollButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanRoll) return;
            var diceToRoll = Dice.Where(d => !d.IsLocked).ToList();

            // 2. ЕФЕКТ "BLINK" (СХОВАТИ) - лише для них
            foreach (var die in diceToRoll)
            {
                die.Visibility = Visibility.Hidden;
            }

            await Task.Delay(150); // Коротка пауза

            // 3. ЛОГІКА КИДКА (Ваш старий код)
            bool isFirstRoll = (RollsLeft == 3);
            RollsLeft--;

            // 4. ОНОВЛЮЄМО ЗНАЧЕННЯ та ПОКАЗУЄМО кубики
            foreach (var die in diceToRoll)
            {
                die.Value = _random.Next(1, 7);
                die.Visibility = Visibility.Visible;
            }

            var currentDice = Dice.Select(d => d.Value).ToList();

            if (isFirstRoll && GameLogic.CheckForInstantWin(currentDice))
            {
                EndGame(PlayerTurn.Human);
                return;
            }

            if (isFirstRoll)
            {
                // Це був перший кидок.
                // Тільки ЗАРАЗ ми вмикаємо кнопки для вибору.
                SetScoringState(true);
            }

            // Оновлюємо потенційні очки
            CalculateAllPotentialScores(currentDice, isFirstRoll);
        }

        private void Die_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTurn != PlayerTurn.Human)
                return;
            // Заборона блокування до першого кидка
            if (RollsLeft == 3)
                return;


            var button = sender as FrameworkElement;
            var die = button.DataContext as DieModel;

            if (die != null)
            {
                die.IsLocked = !die.IsLocked;
                OnPropertyChanged(nameof(CanRoll));
            }
        }

        // --- Логіка гри ---
        private void CalculateAllPotentialScores(List<int> dice, bool isFirstRoll)
        {
            foreach (var entry in Scoreboard.Where(e => e.CategoryName != "Total Score" && !e.HumanFinalScore.HasValue))
            {
                switch (entry.CategoryName)
                {
                    case "Ones": entry.HumanPotentialScore = GameLogic.CalculateSum(dice, 1); break;
                    case "Twos": entry.HumanPotentialScore = GameLogic.CalculateSum(dice, 2); break;
                    case "Threes": entry.HumanPotentialScore = GameLogic.CalculateSum(dice, 3); break;
                    case "Fours": entry.HumanPotentialScore = GameLogic.CalculateSum(dice, 4); break;
                    case "Fives": entry.HumanPotentialScore = GameLogic.CalculateSum(dice, 5); break;
                    case "Sixes": entry.HumanPotentialScore = GameLogic.CalculateSum(dice, 6); break;
                    case "Straight": entry.HumanPotentialScore = GameLogic.CalculateStraight(dice, isFirstRoll); break;
                    case "Full House": entry.HumanPotentialScore = GameLogic.CalculateFullHouse(dice, isFirstRoll); break;
                    case "Four of a kind": entry.HumanPotentialScore = GameLogic.CalculateFourOfAKind(dice, isFirstRoll); break;
                    case "Generala": entry.HumanPotentialScore = GameLogic.CalculateGenerala(dice, isFirstRoll); break;
                }
            }
        }

        private void UpdateTotalScore()
        {
            var totalScoreEntry = Scoreboard.FirstOrDefault(e => e.CategoryName == "Total Score");
            if (totalScoreEntry != null)
            {
                // Рахуємо суму всіх `HumanFinalScore`, ігноруючи 'null'
                int total = Scoreboard.Where(e => e.CategoryName != "Total Score")
                                      .Sum(e => e.HumanFinalScore ?? 0);
                totalScoreEntry.HumanFinalScore = total;
            }
        }

        private void SetScoringState(bool isEnabled)
        {
            foreach (var entry in Scoreboard.Where(e => !e.HumanFinalScore.HasValue))
            {
                entry.IsScoringEnabled = isEnabled;
            }
        }

        private async Task EndTurn()
        {
            foreach (var die in Dice)
            {
                die.IsLocked = false;
            }

            // 1. Викликаємо новий метод, щоб вимкнути всі зелені кнопки           
            SetScoringState(false);

            // 2. Очищуємо всі потенційні очки ("0") з комірок,
            //    щоб таблиця була чистою для ходу комп'ютера
            foreach (var entry in Scoreboard)
            {
                entry.HumanPotentialScore = 0;
            }

            // "Вимикаємо" всі кнопки, очищуючи потенційні очки
            // і передаючи хід
            CurrentTurn = PlayerTurn.Computer;

            bool isGameOver = await PlayComputerTurn();
            if (isGameOver) 
                return;

            if (CurrentRound == TotalRounds)
            {
                // Це був останній хід. НЕ збільшуємо раунд.
                // Одразу завершуємо гру.
                EndGame();
            }
            else
            {
                
                // Перехід на настунпий раунд
                CurrentRound++;

                // Повертаємо хід людині
                CurrentTurn = PlayerTurn.Human;                
                RollsLeft = 3;
            }
        }

        private async Task<bool> PlayComputerTurn()
        {
            // Розблоковуємо усі раніше утримані кубики
            foreach (var die in Dice)
            {
                die.IsLocked = false;
            }

            RollsLeft = 3;
            bool isFirstRoll = true;

            while (RollsLeft > 0)
            {
                // Пауза та ефект "Blink"
                await Task.Delay(700); 
                var diceToRoll = Dice.Where(d => !d.IsLocked).ToList();

                foreach (var die in diceToRoll)
                {
                    die.Visibility = Visibility.Hidden;
                }

                await Task.Delay(150);

                // 1b. Кидок та оновлення UI
                RollsLeft--; 
                foreach (var die in diceToRoll)
                {
                    die.Value = _random.Next(1, 7);
                    die.Visibility = Visibility.Visible;
                }

                var currentDice = Dice.Select(d => d.Value).ToList();

                // Відображення потенційних очок
                CalculateAllComputerScores(currentDice, isFirstRoll);
                if (isFirstRoll)
                {
                    SetComputerScoringState(true);
                }
                await Task.Delay(1000); // Пауза, щоб гравець побачив кидок

                // Перевірка на миттєвий виграш
                if (isFirstRoll && GameLogic.CheckForInstantWin(currentDice))
                {
                    await Task.Delay(1000);
                    EndGame(PlayerTurn.Computer);
                    SetComputerScoringState(false);
                    return true;
                }                

                // Якщо кидків не залишилось, виходимо з циклу
                if (RollsLeft == 0)
                {
                    break;
                }

                // Оцінка того чи варто зупинитися
                await Task.Delay(500);
                double stopValue = 0;
                foreach (var entry in Scoreboard.Where(e => !e.ComputerFinalScore.HasValue && e.CategoryName != "Total Score"))
                {
                    int score = GameLogic.CalculateScoreForEntry(entry.CategoryName, currentDice, isFirstRoll);
                    if (score > stopValue) stopValue = score;
                }
                
                // Оцінка комбінації утримання, яка принесе найбільше очок
                (List<int> holdCombination, double rollValue) = await Task.Run(() =>
                    GameLogic.FindBestDiceToHold_MonteCarlo(
                        currentDice, this.Scoreboard, this.CurrentRound, RollsLeft)
                );

                if (rollValue > stopValue) // Якщо вигідніше кидати далі
                {                   
                    
                    // Утримання кубиків
                    var tempHoldList = new List<int>(holdCombination);
                    foreach (var die in Dice)
                    {
                        if (tempHoldList.Contains(die.Value))
                        {
                            die.IsLocked = true;
                            tempHoldList.Remove(die.Value);
                        }
                        else
                        {
                            die.IsLocked = false;
                        }
                    }

                    await Task.Delay(700);
                }
                else // Якщо кидати не вигідно, то зупиняємось
                {                    
                    break;
                }
                isFirstRoll = false;
            }

            // Запис очок у комірку
            await Task.Delay(1000);

            var finalDice = Dice.Select(d => d.Value).ToList();
            bool wasFinalRollTheFirstRoll = (RollsLeft == 2);

            // Визначаємо найкращу категорію для записц
            (ScoreEntry bestEntry, double expectedValue) = await Task.Run(() =>
                GameLogic.FindBestCategory_MonteCarlo(finalDice, this.Scoreboard, this.CurrentRound, wasFinalRollTheFirstRoll));           

            // Записуємо рахунок
            if (bestEntry != null)
            {
                bestEntry.ComputerPotentialScore = GameLogic.CalculateScoreForEntry(
                    bestEntry.CategoryName,
                    finalDice,
                    wasFinalRollTheFirstRoll
                );

                bestEntry.ComputerFinalScore = bestEntry.ComputerPotentialScore;
                UpdateComputerTotalScore();
            }

            // Прибираємо зелені комірки
            SetComputerScoringState(false);
            foreach (var die in Dice)
            {
                die.IsLocked = false;
            }
            return false;
        }

        private void CalculateAllComputerScores(List<int> dice, bool isFirstRoll)
        {
            // Проходимо по всіх вільних комірках і записуємо потенційний рахунок
            foreach (var entry in Scoreboard.Where(e => !e.ComputerFinalScore.HasValue))
            {
                switch (entry.CategoryName)
                {
                    case "Ones": entry.ComputerPotentialScore = GameLogic.CalculateSum(dice, 1); break;
                    case "Twos": entry.ComputerPotentialScore = GameLogic.CalculateSum(dice, 2); break;
                    case "Threes": entry.ComputerPotentialScore = GameLogic.CalculateSum(dice, 3); break;
                    case "Fours": entry.ComputerPotentialScore = GameLogic.CalculateSum(dice, 4); break;
                    case "Fives": entry.ComputerPotentialScore = GameLogic.CalculateSum(dice, 5); break;
                    case "Sixes": entry.ComputerPotentialScore = GameLogic.CalculateSum(dice, 6); break;
                    case "Straight": entry.ComputerPotentialScore = GameLogic.CalculateStraight(dice, isFirstRoll); break;
                    case "Full House": entry.ComputerPotentialScore = GameLogic.CalculateFullHouse(dice, isFirstRoll); break;
                    case "Four of a kind": entry.ComputerPotentialScore = GameLogic.CalculateFourOfAKind(dice, isFirstRoll); break;
                    case "Generala": entry.ComputerPotentialScore = GameLogic.CalculateGenerala(dice, isFirstRoll); break;
                    default: entry.ComputerPotentialScore = 0; break;
                }
            }
        }
        private void SetComputerScoringState(bool isEnabled)
        {
            foreach (var entry in Scoreboard.Where(e => !e.ComputerFinalScore.HasValue))
            {
                entry.IsComputerScoringEnabled = isEnabled;
            }
        }

        private void UpdateComputerTotalScore()
        {
            var totalScoreEntry = Scoreboard.FirstOrDefault(e => e.CategoryName == "Total Score");
            if (totalScoreEntry != null)
            {
                // Рахуємо суму всіх `ComputerFinalScore`, ігноруючи 'null'
                int total = Scoreboard.Where(e => e.CategoryName != "Total Score")
                                        .Sum(e => e.ComputerFinalScore ?? 0);

                totalScoreEntry.ComputerFinalScore = total;
            }
        }
        // У файлі MainWindow.xaml.cs

        private void EndGame(PlayerTurn? forcedWinner = null)
        {
            string finalMessage;

            if (forcedWinner.HasValue)
            {
                // --- СЦЕНАРІЙ 1: МИТТЄВИЙ ВИГРАШ ---
                if (forcedWinner == PlayerTurn.Human)
                {
                    finalMessage = "GENERAAAALA!\n\nВи виграли на першому ж кидку!";
                }
                else // (forcedWinner == PlayerTurn.Computer)
                {
                    finalMessage = "GENERAAAALA!\n\nКомп'ютер виграв на першому ж кидку!";
                }
            }
            else
            {
                // --- СЦЕНАРІЙ 2: СТАНДАРТНИЙ КІНЕЦЬ ГРИ (порівняння очок) ---
                var totalScoreEntry = Scoreboard.First(e => e.CategoryName == "Total Score");
                int humanTotal = totalScoreEntry.HumanFinalScore ?? 0;
                int computerTotal = totalScoreEntry.ComputerFinalScore ?? 0;

                string resultMessage;
                if (humanTotal > computerTotal)
                {
                    resultMessage = "🎉 Переміг гравець! 🎉";
                }
                else if (computerTotal > humanTotal)
                {
                    resultMessage = "💻 Переміг комп'ютер! 💻";
                }
                else
                {
                    resultMessage = "🤝 Нічия! 🤝";
                }

                finalMessage = $"Гру завершено!\n\n" +
                               $"Ваш рахунок: {humanTotal}\n" +
                               $"Рахунок комп'ютера: {computerTotal}\n\n" +
                               $"{resultMessage}";
            }

            // Показуємо MessageBox (спільний для обох сценаріїв)
            MessageBox.Show(finalMessage, "Кінець гри");

            // Вмикаємо кнопку "New Game"
            IsNewGameButtonEnabled = true;
        }
        // --- Керування вікном ---
        private void RulesButton_Click(object sender, RoutedEventArgs e)
        {
            RulesWindow rulesWin = new RulesWindow();
            rulesWin.Owner = this;
            rulesWin.ShowDialog();
        }
        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            // Скидаємо всі ігрові змінні
            CurrentRound = 1;
            RollsLeft = 3;
            CurrentTurn = PlayerTurn.Human;
            IsNewGameButtonEnabled = false;

            // Повністю очищуємо та заповнюємо таблицю і кубики
            InitializeScoreboard();
            InitializeDice();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to exit the program?",
                "Exit Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}