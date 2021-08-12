using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using VRage;

namespace avaness.PluginLoader.GUI
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {
            InitializeComponent();
            MyVRage.Platform.Windows.HideSplashScreen();
            var thread = new Thread(Loop);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void Loop()
        {
            Show();
            while (!Disposing)
            {
                Application.DoEvents();
            }
        }

        public void SetText(string str)
        {
            labelText.Text = str;
        }
    }
}