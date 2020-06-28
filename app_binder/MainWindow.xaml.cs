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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Web.WebSockets;
using System.ComponentModel;
using Reactive.Bindings;
using System.IO;
using MessagePack;

public enum RESTART_POLICY { NEVER, ON_FAILUER, ALWAYS };

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
                var bytes = File.ReadAllBytes("./config");
                objs = MessagePackSerializer.Deserialize<serialize_objects[]>(bytes);
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
            configs[dataGrid_config.SelectedIndex].kill_config();
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
            byte[] bytes = MessagePackSerializer.Serialize(objs);
            try
            {
                File.WriteAllBytes("./config", bytes);
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

    //[MessagePackObject(keyAsPropertyName: true)]
    public class ProcessChecker : IDisposable
    {
        private bool _disposed = false;

        public Process[] plist;
        public DispatcherTimer timer;
        public Action<int> cb_up;
        public Action<int> cb_down;
        public int pnum { get; set; }
        public string process_name { get; set; }
        public ProcessChecker(string pname)
        {
            process_name = pname;
            pnum = get_number_of_process();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            if (process_name != "")
            {
                plist = Process.GetProcessesByName(process_name);
            }
            if (plist != null)
            {
                if (pnum < plist.Length)
                {
                    cb_up(plist.Length);
                }
                else if (pnum > plist.Length)
                {
                    cb_down(plist.Length);
                }
                pnum = plist.Length;
            }
        }
        public int get_number_of_process()
        {
            if (process_name != "")
            {
                plist = Process.GetProcessesByName(process_name);
                return plist.Length;
            }
            else
            {
                return 0;
            }
        }
        public void stop()
        {
            timer.Stop();
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of unmanaged resources used by the <see cref="ProcessChecker"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ProcessChecker"/> and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //dispose managed resources
                foreach (var p in plist)
                {
                    p?.Dispose();
                }
            }

            _disposed = true;
        }
    }
    [MessagePackObject(keyAsPropertyName: true)]
    public struct serialize_objects
    {
        public bool is_enable;
        public string config_name;
        public string status;
        public string trigger_process;
        public string bind_process;
        public string args;
        public RESTART_POLICY restarter;
    }
    //[MessagePackObject(keyAsPropertyName: true)]
    public class ProcessRunner : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public ProcessChecker PC = new ProcessChecker("");
        // Binding source
        public ReactiveProperty<bool> is_enable { get; set; } = new ReactiveProperty<bool>();
        public ReactiveProperty<string> config_name { get; set; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> status { get; set; } = new ReactiveProperty<string>();

        // Others
        public Process binding_process;
        public string trigger_name { get; set; }
        public RESTART_POLICY restarter { get; set; }
        public ProcessRunner()
        {
            binding_process = new Process();
            binding_process.EnableRaisingEvents = true;
            binding_process.Exited += new EventHandler(proc_Exited);
        }

        public void setup(string name, string trigger_exe, string run_exe, string args, RESTART_POLICY mode)
        {
            config_name.Value = name;
            trigger_name = System.IO.Path.GetFileNameWithoutExtension(trigger_exe);
            if (binding_process.StartInfo.FileName != "" && binding_process.StartInfo.FileName != run_exe) // already running something
            {
                binding_process.Kill();
            }
            binding_process.StartInfo.FileName = run_exe;
            binding_process.StartInfo.Arguments = args;
            binding_process.StartInfo.UseShellExecute = false;
            try
            {
                binding_process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(run_exe);
            }
            catch (Exception)
            {
                binding_process.StartInfo.WorkingDirectory = "./";
            }
            restarter = mode;

            PC.process_name = trigger_name;
            PC.cb_up = trigger_up;
            PC.cb_down = trigger_down;
            if (PC.get_number_of_process() > 0 && is_enable.Value == true)
            {
                start();
            }
            else
            {
                status.Value = "Waiting Trigger Process...";
            }
        }
        public void load_config(serialize_objects obj)
        {
            setup(obj.config_name, obj.trigger_process, obj.bind_process, obj.args, obj.restarter);
            is_enable.Value = true;
        }

        public void trigger_up(int pnum)
        {
            if (pnum == 1 && is_enable.Value == true)
            {
                start();
            }
        }
        public void trigger_down(int pnum)
        {
            if (pnum == 0 && status.Value != "Exited")
            {
                binding_process.Kill();
                status.Value = "Exited";
            }
        }
        public void start()
        {
            bool success;
            try
            {
                success = binding_process.Start();
            }
            catch (Exception e)
            {
                status.Value = e.Message;
                return;
            }
            if (success) status.Value = "Running";
            else
            {
                var p = System.Diagnostics.Process.GetProcessesByName(binding_process.ProcessName);
                Console.WriteLine($"Process:{p.Length}");
                if (p.Length > 0) status.Value = "Reused a process";
                else status.Value = "Error occured";
            }
        }

        private void proc_Exited(object sender, EventArgs e)
        {
            if (binding_process.ExitCode != 0)
            {
                status.Value = $"Fault Detected:{binding_process.ExitCode}";
                if (is_enable.Value == true && (restarter == RESTART_POLICY.ON_FAILUER || restarter == RESTART_POLICY.ALWAYS) && PC.get_number_of_process() != 0)
                {
                    start();
                }
            }
            else
            {
                status.Value = "Exited";
                if (is_enable.Value == true && restarter == RESTART_POLICY.ALWAYS && PC.get_number_of_process() != 0)
                {
                    start();
                }
            }
        }
        public void kill_config()
        {
            PC.stop();
        }
        public serialize_objects serialize()
        {
            serialize_objects sobj;
            sobj.is_enable = is_enable.Value;
            sobj.config_name = config_name.Value;
            sobj.status = status.Value;
            sobj.trigger_process = trigger_name;
            sobj.bind_process = binding_process.StartInfo.FileName;
            sobj.args = binding_process.StartInfo.Arguments;
            sobj.restarter = restarter;
            return sobj;
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of unmanaged resources used by the <see cref="ProcessRunner"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ProcessRunner"/> and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //dispose managed resources
                is_enable?.Dispose();
                config_name?.Dispose();
                status?.Dispose();
                binding_process?.Dispose();
                PC?.Dispose();
            }

            _disposed = true;
        }
    }
}
