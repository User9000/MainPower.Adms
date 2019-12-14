using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MainPower.Adms.IdfManager
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        MainViewModel _model;
        public SettingsWindow(MainViewModel model)
        {
            InitializeComponent();
            this.DataContext = _model = model;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Util.SerializeNewtonsoft("settings.json", _model.Settings);
            this.Close();
        }
    }
}
