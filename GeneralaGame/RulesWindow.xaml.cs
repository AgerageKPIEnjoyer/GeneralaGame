using System.Windows;

namespace GeneralaGame
{
    public partial class RulesWindow : Window
    {
        public RulesWindow()
        {
            InitializeComponent();
        }
       
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {           
            this.Close();
        }
    }
}