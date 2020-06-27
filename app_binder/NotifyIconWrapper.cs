using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AppBinder
{
    public partial class NotifyIconWrapper : Component
    {
        public NotifyIconWrapper()
        {
            InitializeComponent();
            this.toolStripMenuItem_Open.Click += this.toolStripMenuItem_Open_Click;
            this.toolStripMenuItem_Exit.Click += this.toolStripMenuItem_Exit_Click;
        }

        public NotifyIconWrapper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        private void toolStripMenuItem_Open_Click(object sender, EventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void toolStripMenuItem_Exit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}
