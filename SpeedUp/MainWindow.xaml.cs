using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpeedUp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _moduleId = "";
        public MainWindow()
        {
            InitializeComponent();
            FocusManager.SetFocusedElement(this, textModuleID);
        }

        public string ModuleId
        {
            get { return _moduleId; }
            set { _moduleId = value; }
        }

        private void textModuleID_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(textModuleID.Text.Equals("Module ID"))
                textModuleID.Text = "";
        }

        private void LetsStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textModuleID.Text.Trim()))
                    return;

                string workingDirectory = Properties.Settings.Default.TFSLocalFolder;
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    Setting setting = new Setting();
                    setting.ShowDialog();
                }
                if (textModuleID.Text.ToLower().Equals("new"))
                {
                    var newConvertView = new NewConvert();
                    newConvertView.ShowDialog();
                    textModuleID.Text = newConvertView.ModuleId;
                }
                var workNow = new WorkNow(this, textModuleID.Text.Trim());
                this.Hide();
                if (!string.IsNullOrEmpty(Properties.Settings.Default.TFSLocalFolder))
                    workNow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void textModuleID_OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                LetsStart_Click(sender, null);
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            CredentialGathering CredentialGathering = new CredentialGathering();
            CredentialGathering.ShowDialog();
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            CredentialGathering CredentialGathering = new CredentialGathering();
            CredentialGathering.ShowDialog();
        }
    }
}
