using System;
using System.Collections.Generic;
using System.IO;
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
    /// Keyboard.xaml 的交互逻辑
    /// </summary>
    public partial class Keyboard : Window
    {
        public Keyboard()
        {
            InitializeComponent();
            foreach (System.Windows.Forms.Keys k in Enum.GetValues(typeof(System.Windows.Forms.Keys)))
            {
                combo.Items.Add(k);
            }
            combo.Items.Refresh();
            combo.SelectionChanged += delegate (object s, SelectionChangedEventArgs e)
            {
                if (combo.SelectedIndex == 0) return;
                if (list.Items.Count >= 8)
                {
                    MessageBox.Show("8 Keys Maximum.");
                    return;
                }
                list.Items.Add(combo.SelectedItem);
                list.Items.Refresh();
                combo.SelectedIndex = 0;
            };
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedIndex >= 0)
            {
                btnRemove.IsEnabled = true;
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            list.Items.RemoveAt(list.SelectedIndex);
            list.SelectedIndex = -1;
        }
        public string Result
        {
            get
            {
                StringBuilder @string = new StringBuilder();
                foreach (object o in list.Items)
                {
                    if (!(o is System.Windows.Forms.Keys)) continue;
                    System.Windows.Forms.Keys k = (System.Windows.Forms.Keys)o;
                    @string.Append((int)k);
                    @string.Append(" ");
                }
                return @string.ToString().Trim();
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
