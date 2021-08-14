using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace avaness.PluginLoader.GUI
{
    public class SplashScreenLabel
    {
        private readonly Label lbl;
        private readonly ProgressBar bar;
        private readonly Form form;
        private readonly MethodInfo draw;

        public SplashScreenLabel()
        {
            form = LoaderTools.GetMainForm();
            Size formSize = new Size(form.Width, form.Height);

            bar = new ProgressBar()
            {
                Name = "PluginLoaderProgress",
                MaximumSize = formSize,
                Dock = DockStyle.Top,
                Style = ProgressBarStyle.Continuous
            };
            form.Controls.Add(bar);

            lbl = new Label
            {
                Name = "PluginLoaderInfo",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                MaximumSize = formSize,
                Dock = DockStyle.Top,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            form.Controls.Add(lbl);

            draw = form.GetType().GetMethod("Draw", BindingFlags.Instance | BindingFlags.NonPublic);
            SetText("");
        }

        public void SetText(string msg)
        {
            lbl.Text = "[Plugin Loader] " + msg;
            if(draw != null)
                draw.Invoke(form, new object[0]);
            bar.Value = 0;
            bar.Visible = false;
            Application.DoEvents();
        }

        public void SetValue(float percent)
        {
            int newValue = (int)(percent * 100);
            if (newValue == bar.Value)
                return;
            bar.Value = newValue;
            bar.Visible = true;
            if (draw != null)
                draw.Invoke(form, new object[0]);
            Application.DoEvents();
        }

        public void Delete()
        {
            form.Controls.Remove(lbl);
            form.Controls.Remove(bar);
            if (draw != null)
                draw.Invoke(form, new object[0]);
            Application.DoEvents();
        }
    }
}
