using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TxtPilot
{
    public partial class NewFileWindow : Window
    {
        public string FileName { get; private set; } = string.Empty;
        public NewFileWindow()
        {
            InitializeComponent();

            Loaded += (sender, e) =>
            {
                FileNameInput.Focus();
                Keyboard.Focus(FileNameInput);
            };
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            FileName = FileNameInput.Text.Trim();

            if (FileName == string.Empty)
            {
                MessageBox.Show("Please enter a file name");
                return;
            }

            DialogResult = true;
        }
    }
}
