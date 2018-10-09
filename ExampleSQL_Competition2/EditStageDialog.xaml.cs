using System;
using System.Collections;
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

namespace ExampleSQL_Competition2
{
    /// <summary>
    /// Interaction logic for EditStageDialog.xaml
    /// </summary>
    public partial class EditStageDialog : Window
    {
        public EditStageDialog()
        {
            InitializeComponent();
        }

        public EditStageDialog(String name, ArrayList checkpoints)
        {
            InitializeComponent();
            StageNameLbl.Content = name;
            beginCP.ItemsSource =  checkpoints;
            finishCP.ItemsSource = checkpoints;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            beginCP.Focus();
          
        }



        public string Answer
        {
            get
            {
                return beginCP.SelectedItem.ToString() + "," + finishCP.SelectedItem.ToString();
            }

        }

        private void OKbutton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancelbutton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
