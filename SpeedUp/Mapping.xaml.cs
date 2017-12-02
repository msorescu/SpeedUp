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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SpeedUp
{
    /// <summary>
    /// Interaction logic for Mapping.xaml
    /// </summary>
    public partial class Mapping : Window
    {
        public ObservableCollection<MetaData> MetaDataCollection
        { get { return _MetaDataCollection; } }
        ObservableCollection<MetaData> _MetaDataCollection =
        new ObservableCollection<MetaData>();

        public Mapping()
        {
            InitializeComponent();
        }
    }

    public class MetaData
    {
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string BoardType { get; set; }
        public string PropertyClass { get; set; }
        public string Version { get; set; }
    }
}
