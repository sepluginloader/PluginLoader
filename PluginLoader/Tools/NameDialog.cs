using System;
using Sandbox;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Utils;
using VRageMath;

// ReSharper disable VirtualMemberCallInConstructor
#pragma warning disable 618

namespace avaness.PluginLoader.Tools
{
    class NameDialog : MyGuiScreenDebugBase
    {
        private MyGuiControlTextbox nameBox;
        private MyGuiControlButton okButton;
        private MyGuiControlButton cancelButton;

        private readonly Action<string> onOk;

        private readonly string caption;
        private readonly string defaultName;
        private readonly int maxLength;

        public NameDialog(
            Action<string> onOk,
            string caption = "Name",
            string defaultName = "",
            int maxLength = 40)
            : base(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.28f), MyGuiConstants.SCREEN_BACKGROUND_COLOR * MySandboxGame.Config.UIBkOpacity, true)
        {
            this.onOk = onOk;
            this.caption = caption;
            this.defaultName = defaultName;
            this.maxLength = maxLength;

            RecreateControls(true);

            CanBeHidden = true;
            CanHideOthers = true;
            CloseButtonEnabled = true;

            OnEnterCallback = ReturnOk;
        }

        private Vector2 DialogSize => m_size ?? Vector2.One;

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(caption, Color.White.ToVector4(), new Vector2(0.0f, 0.003f));

            var controlSeparatorList1 = new MyGuiControlSeparatorList();
            controlSeparatorList1.AddHorizontal(new Vector2(-0.39f * DialogSize.X, -0.5f * DialogSize.Y + 0.075f), DialogSize.X * 0.78f);
            Controls.Add(controlSeparatorList1);

            var controlSeparatorList2 = new MyGuiControlSeparatorList();
            controlSeparatorList2.AddHorizontal(new Vector2(-0.39f * DialogSize.X, +0.5f * DialogSize.Y - 0.123f), DialogSize.X * 0.78f);
            Controls.Add(controlSeparatorList2);

            nameBox = new MyGuiControlTextbox(new Vector2(0.0f, -0.027f), maxLength: maxLength)
            {
                Text = defaultName,
                Size = new Vector2(0.385f, 1f)
            };
            nameBox.SelectAll();
            Controls.Add(nameBox);

            okButton = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER, text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOk);
            cancelButton = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancel);

            var okPosition = new Vector2(0.001f, 0.5f * DialogSize.Y - 0.071f);
            var halfDistance = new Vector2(0.018f, 0.0f);

            okButton.Position = okPosition - halfDistance;
            cancelButton.Position = okPosition + halfDistance;

            okButton.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipNewsletter_Ok));
            cancelButton.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipOptionsSpace_Cancel));

            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        private void CallResultCallback(string text)
        {
            if (text == null)
                return;

            onOk(text);
        }

        private void ReturnOk()
        {
            if (nameBox.GetTextLength() <= 0)
                return;

            CallResultCallback(nameBox.Text);
            CloseScreen();
        }

        private void OnOk(MyGuiControlButton button) => ReturnOk();
        private void OnCancel(MyGuiControlButton button) => CloseScreen();

        public override string GetFriendlyName() => "NameDialog";
    }
}