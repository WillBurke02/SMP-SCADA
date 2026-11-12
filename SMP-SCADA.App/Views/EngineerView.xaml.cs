using SMP_SCADA.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SMP_SCADA.Views
{
    /// <summary>
    /// Interaction logic for EngineerView.xaml
    /// </summary>
    public partial class EngineerView : UserControl
    {
        public EngineerView() => InitializeComponent();
        void UnlockClick(object sender, RoutedEventArgs e)
        {
            ((EngineerViewModel)DataContext).Unlock(Pin.Password);
            Pin.Clear();
        }

        void PinKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) UnlockClick(sender, e);
        }
    }
}
