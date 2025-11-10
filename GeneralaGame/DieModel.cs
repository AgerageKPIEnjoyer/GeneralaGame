using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GeneralaGame
{
    public class DieModel : INotifyPropertyChanged
    {        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
       
        private int _value = 1;
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged(); // Повідомити UI
                OnPropertyChanged(nameof(ImagePath)); // Оновити зображенн кубика
            }
        }
        // Властивість для стану кубика (заблокований/ні)
        private bool _isLocked = false;
        public bool IsLocked
        {
            get { return _isLocked; }
            set
            {
                _isLocked = value;
                OnPropertyChanged(); // Повідомити UI
                OnPropertyChanged(nameof(ImagePath)); // Оновити картинку
            }
        }

        //Властивість, що сама обирає правильну картинку (для кубиків)     
        public string ImagePath
        {
            get
            {
                if (IsLocked)
                {
                    // Якщо заблокований, беремо темну картинку
                    return $"/images/gray_dice{Value}.png";
                }
                else
                {
                    // Якщо активний, беремо світлу картинку
                    return $"/images/dice{Value}.png";
                }
            }
        }

        private Visibility _visibility = Visibility.Visible;
        public Visibility Visibility
        {
            get { return _visibility; }
            set { _visibility = value; OnPropertyChanged(); }
        }
    }
}
