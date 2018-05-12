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

namespace TabletDriverGUI
{
    /// <summary>
    /// EffectiveConditionEditor.xaml 的交互逻辑
    /// </summary>
    public partial class EffectiveConditionEditor : Window
    {
        public EffectiveConditionEditor(Configuration.EffectiveCondition e)
        {
            InitializeComponent();
            E = e;
            V.Text = E.V;
            foreach (object o in K.Items)
            {
                if ((o as string).Split('|')[1] == E.K)
                {
                    K.SelectedItem = o;
                }
            }
        }

        public Configuration.EffectiveCondition E { get; }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            E.K = K.SelectedItem.ToString().Split('|')[1];
            E.V = V.Text;
        }
    }
}
