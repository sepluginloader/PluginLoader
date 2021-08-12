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

        /// <summary>
        /// Method to rotate an image either clockwise or counter-clockwise
        /// </summary>
        /// <param name="img">the image to be rotated</param>
        /// <param name="rotationAngle">the angle (in degrees).
        /// NOTE: 
        /// Positive values will rotate clockwise
        /// negative values will rotate counter-clockwise
        /// </param>
        /// <returns></returns>
        public static Image RotateImage(Image img, float rotationAngle)
        {
            //create an empty Bitmap image
            Bitmap bmp = new Bitmap(img.Width, img.Height);

            //turn the Bitmap into a Graphics object
            Graphics gfx = Graphics.FromImage(bmp);

            //now we set the rotation point to the center of our image
            gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);

            //now rotate the image
            gfx.RotateTransform(rotationAngle);

            gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);

            //set the InterpolationMode to HighQualityBicubic so to ensure a high
            //quality image once it is transformed to the specified size
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //now draw our new image onto the graphics object
            gfx.DrawImage(img, new Point(0, 0));

            //dispose of our Graphics object
            gfx.Dispose();

            //return the image
            return bmp;
        }
    }
}
