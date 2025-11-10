using System.Windows;

namespace GeneralaGame
{
    public partial class RulesWindow : Window
    {
        public RulesWindow()
        {
            InitializeComponent();
        }

        // Цей метод закриє вікно при натисканні на кнопку
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Закриває це (RulesWindow) вікно
            this.Close();
        }
    }
}