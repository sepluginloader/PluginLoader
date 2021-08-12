using System.Windows.Forms;
using System.Reflection;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Windows;
using System.Drawing.Drawing2D;

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
            form.Size = new Size(Convert.ToInt32(SystemParameters.VirtualScreenWidth), Convert.ToInt32(SystemParameters.VirtualScreenHeight));
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new System.Drawing.Point(0, 0);
            form.Show();
            lbl = new Label();
            lbl.Location = new System.Drawing.Point(Convert.ToInt32(SystemParameters.VirtualScreenWidth / 2) - (lbl.Width / 2), Convert.ToInt32(SystemParameters.VirtualScreenHeight / 1.2) - (lbl.Height / 2));
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.AutoSize = true;
            lbl.BackColor = System.Drawing.Color.Transparent;
            lbl.ForeColor = System.Drawing.Color.White;
            lbl.Font = new Font(FontFamily.GenericSansSerif, 25, FontStyle.Regular);
            form.Controls.Add(lbl);
            
        }
        
        public void SetText(string msg)
        {

            lbl.Text = "[Plugin Loader]\n" + msg;
            if (draw != null)
                draw.Invoke(form, new object[0]);
            lbl.Location = new System.Drawing.Point(Convert.ToInt32(SystemParameters.VirtualScreenWidth / 2) - (lbl.Width / 2), Convert.ToInt32(SystemParameters.VirtualScreenHeight / 1.2) - (lbl.Height / 2));
            System.Windows.Forms.Application.DoEvents();
        }

        public void Delete()
        {
            form.Controls.Remove(lbl);
        }

    }
}
