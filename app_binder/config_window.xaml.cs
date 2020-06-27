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
using Microsoft.Win32;
using MahApps.Metro.Controls;
using System.IO;
using System.Diagnostics;

namespace AppBinder
{
    public partial class config_window : MetroWindow
    {
        int config_index;
        public config_window()
        {
            InitializeComponent();
            textBox_name.Text = $"Config{((MainWindow)Application.Current.MainWindow).configs.Count + 1}";
        }

        public void load_config(serialize_objects obj, int index)
        {
            button_done.Visibility = Visibility.Hidden;
            button_modify.Visibility = Visibility.Visible;
            textBox_name.Text = obj.config_name;
            textBox_trigger.Text = obj.trigger_process;
            textBox_start.Text = obj.bind_process;
            textBox_args.Text = obj.args;
            set_restart_radio_button(obj.restarter);
            config_index = index;
        }

        private void button_triggerbrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Executable file(*.exe)|*.exe";
            if (dialog.ShowDialog() == true)
            {
                textBox_trigger.Text = dialog.FileName;
            }
        }

        private void button_startbrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Executable file(*.exe)|*.exe";
            if (dialog.ShowDialog() == true)
            {
                textBox_start.Text = dialog.FileName;
            }
        }
        private void set_restart_radio_button(RESTART_POLICY p)
        {
            switch (p)
            {
                case (RESTART_POLICY.NEVER):
                    radioButton_never.IsChecked = true;
                    break;
                case (RESTART_POLICY.ON_FAILUER):
                    radioButton_failure.IsChecked = true;
                    break;
                case (RESTART_POLICY.ALWAYS):
                    radioButton_always.IsChecked = true;
                    break;
            }
        }
        private RESTART_POLICY check_restart_policy()
        {
            if (radioButton_never.IsChecked == true) return RESTART_POLICY.NEVER;
            else if (radioButton_failure.IsChecked == true) return RESTART_POLICY.ON_FAILUER;
            else return RESTART_POLICY.ALWAYS;
        }
        private void button_done_Click(object sender, RoutedEventArgs e)
        {
            var config = new ProcessRunner();
            config.setup(textBox_name.Text, textBox_trigger.Text, textBox_start.Text, textBox_args.Text, check_restart_policy());
            config.is_enable.Value = true;
            ((MainWindow)Application.Current.MainWindow).configs.Add(config);
            this.Close();
        }

        private void button_modify_Click(object sender, RoutedEventArgs e)
        {
            var config = new ProcessRunner();
            config.setup(textBox_name.Text, textBox_trigger.Text, textBox_start.Text, textBox_args.Text, check_restart_policy());
            config.is_enable.Value = true;
            ((MainWindow)Application.Current.MainWindow).configs[config_index].kill_config();
            ((MainWindow)Application.Current.MainWindow).configs[config_index] = config;
            this.Close();
        }
    }

}
