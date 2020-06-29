using System;
using System.Windows;
using System.Collections.ObjectModel;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace AppBinder
{
    public partial class MainWindow : MetroWindow
    {
        public ObservableCollection<ProcessRunner> configs = new ObservableCollection<ProcessRunner>();
        public MainWindow()
        {
            InitializeComponent();
            serialize_objects[] objs;
            try
            {
                var json = File.ReadAllText("./config.json");
                objs = JsonConvert.DeserializeObject<serialize_objects[]>(json);
                foreach (var o in objs)
                {
                    var pc = new ProcessRunner();
                    pc.load_config(o);
                    configs.Add(pc);
                }
            }
            catch (Exception)
            {

            }
            dataGrid_config.ItemsSource = configs;
        }

        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            config_window cw = new config_window();
            cw.Owner = this;
            cw.Show();
        }

        private void button_edit_Click(object sender, RoutedEventArgs e)
        {
            if (configs.Count < 1 || dataGrid_config.SelectedIndex < 0) return;
            config_window cw = new config_window();
            cw.load_config(configs[dataGrid_config.SelectedIndex].serialize(), dataGrid_config.SelectedIndex);
            cw.Owner = this;
            cw.Show();
        }

        private void button_delete_Click(object sender, RoutedEventArgs e)
        {
            if (configs.Count < 1 || dataGrid_config.SelectedIndex < 0) return;
            configs[dataGrid_config.SelectedIndex].Dispose();
            configs.Remove(configs[dataGrid_config.SelectedIndex]);
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            save_configs();
            Application.Current.Shutdown();
        }

        private void save_configs()
        {
            serialize_objects[] objs = new serialize_objects[configs.Count];
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i] = configs[i].serialize();
            }
            var json = JsonConvert.SerializeObject(objs,Formatting.Indented);
            try
            {
                File.WriteAllText("./config.json", json);
            }
            catch (Exception)
            {
            }
        }
        private void c_StateChanged(object sender, EventArgs e)
        {
            save_configs();
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.ShowInTaskbar = true;
            }
        }
    }
}
