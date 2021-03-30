using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace avaness.PluginLoader.GUI
{
    public class SplashScreenLabel
    {
        private Label lbl;
        private Form form;
        private MethodInfo draw;

        public SplashScreenLabel()
        {
            lbl = new Label();
            lbl.Name = "PluginLoaderInfo";
            lbl.Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
            form = LoaderTools.GetMainForm();
            lbl.MaximumSize = new Size(form.Width, form.Height);
            lbl.AutoSize = true;
            form.Controls.Add(lbl);
            draw = form.GetType().GetMethod("Draw", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void SetText(string msg)
        {
            lbl.Text = "[Plugin Loader]\n" + msg;
            if(draw != null)
                draw.Invoke(form, new object[0]);
            Application.DoEvents();
        }

        public void Delete()
        {
            form.Controls.Remove(lbl);
        }
    }
}
