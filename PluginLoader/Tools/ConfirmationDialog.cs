using System;
using System.Text;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.Tools
{
    public static class ConfirmationDialog
    {
        public static MyGuiScreenMessageBox CreateMessageBox(
            MyMessageBoxStyleEnum styleEnum = MyMessageBoxStyleEnum.Error,
            MyMessageBoxButtonsType buttonType = MyMessageBoxButtonsType.OK,
            StringBuilder messageText = null,
            StringBuilder messageCaption = null,
            MyStringId? okButtonText = null,
            MyStringId? cancelButtonText = null,
            MyStringId? yesButtonText = null,
            MyStringId? noButtonText = null,
            Action<MyGuiScreenMessageBox.ResultEnum> callback = null,
            int timeoutInMiliseconds = 0,
            MyGuiScreenMessageBox.ResultEnum focusedResult = MyGuiScreenMessageBox.ResultEnum.YES,
            bool canHideOthers = true,
            Vector2? size = null,
            bool useOpacity = true,
            Vector2? position = null,
            bool focusable = true,
            bool canBeHidden = false,
            Action onClosing = null)
        {
            int num1 = (int)styleEnum;
            int num2 = (int)buttonType;
            StringBuilder messageText1 = messageText;
            StringBuilder messageCaption1 = messageCaption;
            MyStringId? nullable = okButtonText;
            MyStringId okButtonText1 = nullable ?? MyCommonTexts.Ok;
            nullable = cancelButtonText;
            MyStringId cancelButtonText1 = nullable ?? MyCommonTexts.Cancel;
            nullable = yesButtonText;
            MyStringId yesButtonText1 = nullable ?? MyCommonTexts.Yes;
            nullable = noButtonText;
            MyStringId noButtonText1 = nullable ?? MyCommonTexts.No;
            Action<MyGuiScreenMessageBox.ResultEnum> callback1 = callback;
            int timeoutInMiliseconds1 = timeoutInMiliseconds;
            int num3 = (int)focusedResult;
            int num4 = canHideOthers ? 1 : 0;
            Vector2? size1 = size;
            double num5 = useOpacity ? (double)MySandboxGame.Config.UIBkOpacity : 1.0;
            double num6 = useOpacity ? (double)MySandboxGame.Config.UIOpacity : 1.0;
            Vector2? position1 = position;
            int num7 = focusable ? 1 : 0;
            int num8 = canBeHidden ? 1 : 0;
            Action onClosing1 = onClosing;
            var dlg = new MyGuiScreenMessageBox((MyMessageBoxStyleEnum)num1, (MyMessageBoxButtonsType)num2, messageText1, messageCaption1, okButtonText1, cancelButtonText1, yesButtonText1, noButtonText1, callback1, timeoutInMiliseconds1, (MyGuiScreenMessageBox.ResultEnum)num3, num4 != 0, size1, (float)num5, (float)num6, position1, num7 != 0, num8 != 0, onClosing1);

            if (dlg.Controls.GetControlByName("MyGuiControlMultilineText") is MyGuiControlMultilineText text)
                text.TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;

            return dlg;
        }
    }
}