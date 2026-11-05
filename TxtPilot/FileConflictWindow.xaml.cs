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
    public partial class FileConflictWindow : Window
    {
        public bool Overwrite { get; private set; } = false;

        public bool Reload { get; private set; } = false;

        public FileConflictWindow()
        {
            InitializeComponent();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Reload = true;
            DialogResult = true;
        }

        private void OverwriteButton_Click(object sender, RoutedEventArgs e)
        {
            Overwrite = true;
            DialogResult = true;
        }

    }
}
