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

        // Назва категорії 
        public string CategoryName { get; set; }

        // Властивості для гравця-людини
        private int? _humanFinalScore; // Збережений рахунок (nullable)
        public int? HumanFinalScore
        {
            get => _humanFinalScore;
            set 
            {
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
            set 
            { 
                _humanPotentialScore = value;
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HumanDisplayScore));
            }
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

       
        // Показує фінальний рахунок, якщо він є.
        // Інакше показує потенційний рахунок
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
       
        // Властивість для запису значення в комірку
        public bool IsHumanScorable
        {            
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

        // Показує досутпні комірки для комп'ютера
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

        // Контролює стиль кнопки
        public bool IsComputerScorable => !ComputerFinalScore.HasValue && IsComputerScoringEnabled;

        //  Показує потенційний рахунок або фінальний рахунок для комп'ютера
        public string ComputerDisplayScore
        {
            get
            {
                if (ComputerFinalScore.HasValue) 
                    return ComputerFinalScore.Value.ToString();

                // Показуємо потенційний рахунок, якщо комп'ютер "думає"
                if (IsComputerScoringEnabled)
                    return ComputerPotentialScore.ToString();

                return "";
            }
        }

        public ScoreEntry Clone()
        {            
            return this.MemberwiseClone() as ScoreEntry;
        }
    }
}