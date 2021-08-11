using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Windows;

namespace avaness.PluginLoader.GUI
{
    public class SplashScreenLabel
    {
        private Label lbl;
        private Form form;
        private MethodInfo draw;

        public SplashScreenLabel()
        {
            form = LoaderTools.GetMainForm();
            form.Invalidate();
            form.BackgroundImage = Properties.Resources.BGImage;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(form.Width / 2 - form.Width / 2 + form.Location.X,
                                      form.Height / 2 - form.Height / 2 + form.Location.Y);
            form.Show();
            //form.Size = new Size(Convert.ToInt32(SystemParameters.VirtualScreenWidth), Convert.ToInt32(SystemParameters.VirtualScreenHeight));
        }
        
        public void SetText(string msg)
        {

            //lbl.Text = "[Plugin Loader]\n" + msg;
            //if (draw != null)
            //    draw.Invoke(form, new object[0]);
            //Application.DoEvents();
        }

        public void Delete()
        {
            form.Controls.Remove(lbl);
        }
    }
}
