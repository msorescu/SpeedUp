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
using System.Windows.Shapes;

namespace SpeedUp
{
    /// <summary>
    /// Interaction logic for Setting.xaml
    /// </summary>
    public partial class Setting : Window
    {
        public Setting()
        {
            try
            {
                InitializeComponent();
                TFSLocalFolderTextBox.Text = Properties.Settings.Default.TFSLocalFolder;
                ReplyEmailTemplateTextBox.Text = Properties.Settings.Default.ReplyEmailTemplate;
                RETSClientToolLocationTextBox.Text = Properties.Settings.Default.RETSClientToolPath;
                HandyMapperLocationTextBox.Text = Properties.Settings.Default.HandyMapperPath;
                SearchMapperTextBox.Text = Properties.Settings.Default.SearchToolPath;
                TestHarnessTextBox.Text = Properties.Settings.Default.TestHarnessPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateLocalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TFSLocalFolderTextBox.Text.EndsWith("TCDEF_a.1_Main"))
            {
                MessageBox.Show("Folder must end with \"TCDEF_a.1_Main\"");
                return;
            }
            Properties.Settings.Default.TFSLocalFolder = TFSLocalFolderTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void UpdateEmailTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ReplyEmailTemplate = ReplyEmailTemplateTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.TFSLocalFolder))
            {
                MessageBox.Show("Please set your local TFS folder.");
                e.Cancel = true;
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TFSLocalFolderTextBox.Text.EndsWith("TCDEF_a.1_Main"))
            {
                MessageBox.Show("Folder must end with \"TCDEF_a.1_Main\"");
                return;
            }
            Properties.Settings.Default.TFSLocalFolder = TFSLocalFolderTextBox.Text;
            Properties.Settings.Default.ReplyEmailTemplate = ReplyEmailTemplateTextBox.Text;
            Properties.Settings.Default.RETSClientToolPath = RETSClientToolLocationTextBox.Text;
            Properties.Settings.Default.HandyMapperPath = HandyMapperLocationTextBox.Text;
            Properties.Settings.Default.SearchToolPath = SearchMapperTextBox.Text;
            Properties.Settings.Default.TestHarnessPath = TestHarnessTextBox.Text;
            Properties.Settings.Default.Save();
        }
    }
}
