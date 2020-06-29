using System;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Reactive.Bindings;
using System.Threading.Tasks;

public enum RESTART_POLICY { NEVER, ON_FAILUER, ALWAYS };

namespace AppBinder
{
    /// <summary>
    /// Check number of process.
    /// Action "cb_up" and "cb_down" acheive callbacks when the monitoring process is upped or downed.
    /// </summary>
    public class ProcessChecker : IDisposable
    {
        private bool _disposed = false;

        private Process[] plist;
        private DispatcherTimer timer;
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
                if (plist != null)
                {
                    foreach (var p in plist)
                    {
                        p?.Dispose();
                    }
                }
            }

            _disposed = true;
        }
    }
    /// <summary>
    /// Serialize object for ProcessRunner
    /// </summary>
    public class serialize_objects
    {
        public bool is_enable { get; set; }
        public string config_name { get; set; }
        public string status { get; set; }
        public string trigger_process { get; set; }
        public string bind_process { get; set; }
        public string args { get; set; }
        public int start_delay { get; set; }
        public RESTART_POLICY restarter { get; set; }
    }
    /// <summary>
    /// Process runner with monitoring
    /// </summary>
    public class ProcessRunner : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed = false;

        public event PropertyChangedEventHandler PropertyChanged;
        private ProcessChecker PC;
        // Binding source
        public ReactiveProperty<bool> is_enable { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<string> config_name { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> status { get; } = new ReactiveProperty<string>();

        // Others
        public Process binding_process;
        public string trigger_name { get; set; }
        public int start_delay { get; set; }
        public RESTART_POLICY restarter { get; set; }
        public ProcessRunner()
        {
            binding_process = new Process();
            binding_process.EnableRaisingEvents = true;
            binding_process.Exited += new EventHandler(proc_Exited);
            PC = new ProcessChecker("");
        }

        public void setup(string name, string trigger_exe, string run_exe, string args, string sdelay, RESTART_POLICY mode)
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
            try
            {
                start_delay = Int32.Parse(sdelay) * 1000;
                if (start_delay < 0) start_delay *= -1;
            }
            catch (FormatException)
            {
                start_delay = 1000;
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
            setup(obj.config_name, obj.trigger_process, obj.bind_process, obj.args, (obj.start_delay / 1000).ToString(), obj.restarter);
            is_enable.Value = true;
        }

        private void trigger_up(int pnum)
        {
            if (pnum == 1 && is_enable.Value == true)
            {
                start();
            }
        }
        private void trigger_down(int pnum)
        {
            if (pnum == 0 && status.Value != "Exited")
            {
                binding_process.Kill();
                status.Value = "Exited";
            }
        }
        public async void start()
        {
            bool success = false;
            try
            {
                await Task.Delay(start_delay);
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
        public serialize_objects serialize()
        {
            var sobj = new serialize_objects();
            sobj.is_enable = is_enable.Value;
            sobj.config_name = config_name.Value;
            sobj.status = status.Value;
            sobj.trigger_process = trigger_name;
            sobj.bind_process = binding_process.StartInfo.FileName;
            sobj.args = binding_process.StartInfo.Arguments;
            sobj.start_delay = start_delay;
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
                PC.stop();
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
