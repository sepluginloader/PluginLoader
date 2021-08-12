using System;
using System.Drawing;
using System.IO;
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
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Load("https://media.discordapp.net/attachments/875374466664906823/875375766148370442/Untitled-1_1.gif");
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