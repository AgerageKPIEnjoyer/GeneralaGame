using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GeneralaGame
{
    public class ScoreEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Назва категорії (Ones, Twos...)
        public string CategoryName { get; set; }

        // --- Властивості для гравця (You) ---
        private int? _humanFinalScore; // Збережений рахунок (nullable)
        public int? HumanFinalScore
        {
            get => _humanFinalScore;
            set {
                _humanFinalScore = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HumanDisplayScore));
                OnPropertyChanged(nameof(IsHumanScorable)); 
            }
        }

        private int _humanPotentialScore; // Потенційний рахунок цього кидка
        public int HumanPotentialScore
        {
            get => _humanPotentialScore;
            set { _humanPotentialScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(HumanDisplayScore)); }
        }

        private bool _isScoringEnabled = false;
        public bool IsScoringEnabled
        {
            get => _isScoringEnabled;
            set
            {
                _isScoringEnabled = value;
                OnPropertyChanged(nameof(IsHumanScorable));
            }
        }

        // "Розумна" властивість, яку бачить UI (кнопка).
        // Показує фінальний рахунок, ЯКЩО він є.
        // Інакше показує потенційний рахунок (якщо він > 0).
        public string HumanDisplayScore
        {
            get
            {               
                if (HumanFinalScore.HasValue)
                {
                    return HumanFinalScore.Value.ToString();
                }
                
                if (_isScoringEnabled)
                {
                    return HumanPotentialScore.ToString();
                }
               
                return "";
            }
        }      
       
        public bool IsHumanScorable
        {
            // Кнопка активна, ТІЛЬКИ ЯКЩО:
            // 1. Рахунок ще не записаний
            // 2. MainWindow ("мозок" гри) дозволив вибір
            get => !HumanFinalScore.HasValue && IsScoringEnabled;
        }

        private int? _computerFinalScore;
        public int? ComputerFinalScore
        {
            get => _computerFinalScore;
            set 
            { 
                _computerFinalScore = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ComputerDisplayScore)); 
                OnPropertyChanged(nameof(IsComputerScorable)); 
            }
        }

        private int _computerPotentialScore;
        public int ComputerPotentialScore
        {
            get => _computerPotentialScore;
            set 
            { 
                _computerPotentialScore = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ComputerDisplayScore)); 
            }
        }

        // НОВА ВЛАСТИВІСТЬ: Вмикає/вимикає зелений колір
        private bool _isComputerScoringEnabled = false;
        public bool IsComputerScoringEnabled
        {
            get => _isComputerScoringEnabled;
            set 
            { 
                _isComputerScoringEnabled = value; 
                OnPropertyChanged(nameof(IsComputerScorable));
                OnPropertyChanged(nameof(ComputerDisplayScore));
            }
        }

        // НОВА ВЛАСТИВІСТЬ: Контролює стиль кнопки
        public bool IsComputerScorable => !ComputerFinalScore.HasValue && IsComputerScoringEnabled;

        // ОНОВЛЕНА ВЛАСТИВІСТЬ: Показує потенційний рахунок або фінальний
        public string ComputerDisplayScore
        {
            get
            {
                if (ComputerFinalScore.HasValue) return ComputerFinalScore.Value.ToString();
                // Показуємо потенційний рахунок, якщо ШІ "думає"
                if (IsComputerScoringEnabled) return ComputerPotentialScore.ToString();
                return "";
            }
        }

        public ScoreEntry Clone()
        {
            // MemberwiseClone створює копію. 
            // Це ідеально для нашої симуляції.
            return this.MemberwiseClone() as ScoreEntry;
        }
    }
}