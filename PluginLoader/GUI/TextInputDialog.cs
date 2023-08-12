using System;
using System.Text;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI
{
    public class TextInputDialog : PluginScreen
	{
        private readonly string title;
        private string text;
        private readonly Action<string> onComplete;

        public TextInputDialog(string title, string defaultText = null, Action<string> onComplete = null) : base(size: new Vector2(0.45f, 0.25f))
		{
            this.title = title;
            text = defaultText;
            this.onComplete = onComplete;
        }

		public override string GetFriendlyName()
		{
			return typeof(TextInputDialog).FullName;
		}

		public override void RecreateControls(bool constructor)
		{
			base.RecreateControls(constructor);

            MyGuiControlLabel caption = AddCaption(title);

			Vector2 bottomMid = new Vector2(0, m_size.Value.Y / 2);
			MyGuiControlButton btnApply = new MyGuiControlButton(position: bottomMid - GuiSpacing, text: new StringBuilder("Ok"), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, onButtonClick: OnOkClick);
			MyGuiControlButton btnCancel = new MyGuiControlButton(text: new StringBuilder("Cancel"), onButtonClick: OnCancelClick);
			PositionToRight(btnApply, btnCancel, spacing: GuiSpacing * 2);
			Controls.Add(btnApply);
			Controls.Add(btnCancel);

			MyGuiControlTextbox textbox = new MyGuiControlTextbox(defaultText: text);
			textbox.TextChanged += OnTextChanged;
			Controls.Add(textbox);
			textbox.SelectAll();
			FocusedControl = textbox;
		}

        private void OnTextChanged(MyGuiControlTextbox textbox)
        {
			text = textbox.Text;
        }

        private void OnCancelClick(MyGuiControlButton btn)
        {
			CloseScreen();
        }

        private void OnOkClick(MyGuiControlButton obj)
        {
			if (!string.IsNullOrWhiteSpace(text))
				onComplete?.Invoke(text);
			CloseScreen();
        }
	}

}
