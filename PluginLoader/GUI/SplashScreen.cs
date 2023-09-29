using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.IO;
using VRage;
using Sandbox.Game;

namespace avaness.PluginLoader.GUI
{
    public class SplashScreen : Form
	{
		private const float barWidth = 0.98f; // 98% of width
		private const float barHeight = 0.06f; // 6% of height
		private static readonly Color backgroundColor = Color.FromArgb(4, 4, 4);

		private readonly bool invalid;
		private readonly Label lbl;
		private readonly PictureBox gifBox;
		private readonly RectangleF bar;

		private float barValue = float.NaN;

        public object GameInfo { get; private set; }

        public SplashScreen()
        {
			Image gif;
			if (Application.OpenForms.Count == 0 || !TryLoadImage(out gif))
			{
				invalid = true;
				return;
			}

			Form defaultSplash = Application.OpenForms[0];
			Size = defaultSplash.Size;
			ClientSize = defaultSplash.ClientSize;
			MyVRage.Platform.Windows.HideSplashScreen();

			Name = "SplashScreenPluginLoader";
			FormBorderStyle = FormBorderStyle.None;

			SizeF barSize = new SizeF(Size.Width * barWidth, Size.Height * barHeight);
			float padding = (1 - barWidth) * Size.Width * 0.5f;
			PointF barStart = new PointF(padding, Size.Height - barSize.Height - padding);
			bar = new RectangleF(barStart, barSize);

			Font lblFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
			lbl = new Label
			{
				Name = "PluginLoaderInfo",
				Font = lblFont,
				BackColor = backgroundColor,
				ForeColor = Color.White,
				MaximumSize = Size,
				Size = new Size(Size.Width, lblFont.Height),
				TextAlign = ContentAlignment.MiddleCenter,
				Location = new Point(0, (int)(barStart.Y - lblFont.Height - 1)),
			};
			Controls.Add(lbl);

			gifBox = new PictureBox()
			{
				Name = "PluginLoaderAnimation",
				Image = gif,
				Size = Size,
				AutoSize = false,
				SizeMode = PictureBoxSizeMode.StretchImage,
			};
			Controls.Add(gifBox);

			gifBox.Paint += OnPictureBoxDraw;

			CenterToScreen();
			Show();
			ForceUpdate();
		}

        private bool TryLoadImage(out Image img)
		{
			try
			{
				Assembly myAssembly = Assembly.GetExecutingAssembly();
				Stream myStream = myAssembly.GetManifestResourceStream("avaness.PluginLoader.splash.gif");
				img = new Bitmap(myStream);
				return true;
			}
			catch 
			{
				img = null;
				return false;
			}
		}

		public void SetText(string msg)
		{
			if (invalid)
				return;
			
			lbl.Text = msg;
			barValue = float.NaN;
			gifBox.Invalidate();
			ForceUpdate();
		}

		public void SetBarValue(float percent = float.NaN)
		{
			if (invalid)
				return;

			barValue = percent;
			gifBox.Invalidate();
			ForceUpdate();
		}

		private void ForceUpdate()
		{
			Application.DoEvents();
		}

		private void OnPictureBoxDraw(object sender, PaintEventArgs e)
		{
			if (!float.IsNaN(barValue))
			{
				Graphics graphics = e.Graphics;
				graphics.FillRectangle(Brushes.DarkSlateGray, bar);
				graphics.FillRectangle(Brushes.White, new RectangleF(bar.Location, new SizeF(bar.Width * barValue, bar.Height)));
			}
		}

		public void Delete()
        {
			if (invalid)
				return;

			gifBox.Paint -= OnPictureBoxDraw;
			Close();
			Dispose();
			ForceUpdate();
			MyVRage.Platform.Windows.ShowSplashScreen(MyPerGameSettings.BasicGameInfo.SplashScreenImage, new VRageMath.Vector2(0.7f, 0.7f));
		}
	}
}
