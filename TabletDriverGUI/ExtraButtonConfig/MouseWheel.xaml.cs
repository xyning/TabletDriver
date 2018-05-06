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

namespace TabletDriverGUI.ExtraButtonConfig
{
    /// <summary>
    /// MouseWheel.xaml 的交互逻辑
    /// </summary>
    public partial class MouseWheel : Window
    {
        public MouseWheel()
        {
            InitializeComponent();
        }
        private int value = 20;
        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                label.Content = Value;
            }
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int i = (int)slider.Value;
            Value = i;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Slider_Loaded(object sender, RoutedEventArgs e)
        {
            slider.ValueChanged += Slider_ValueChanged;
        }
    }
}
