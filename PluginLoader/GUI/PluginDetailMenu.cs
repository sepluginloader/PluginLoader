using avaness.PluginLoader.Data;
using avaness.PluginLoader.Stats;
using avaness.PluginLoader.Stats.Model;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI
{
    class PluginDetailMenu : PluginScreen
    {
        private PluginData plugin;
        private PluginInstance pluginInstance;
        private PluginStat stats;

        public PluginDetailMenu(PluginData plugin) : base(size: new Vector2(0.5f, 0.8f))
        {
            this.plugin = plugin;
            if (Main.Instance.TryGetPluginInstance(plugin.Id, out PluginInstance instance))
                pluginInstance = instance;
            PluginStats stats = Main.Instance.Stats;
            if (stats != null && !stats.Stats.TryGetValue(plugin.Id, out this.stats))
                this.stats = new PluginStat();
        }

        public override string GetFriendlyName()
        {
            return nameof(PluginDetailMenu);
        }

        public override void RecreateControls(bool constructor)
        {
            // Top
            MyGuiControlLabel caption = AddCaption("Plugin Details", captionScale: 1);
            AddBarBelow(caption);

            // Bottom
            Vector2 halfSize = m_size.Value / 2;
            MyGuiControlLabel lblSource = new MyGuiControlLabel(text: plugin.Source, position: new Vector2(GuiSpacing - halfSize.X, halfSize.Y - GuiSpacing), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            Controls.Add(lblSource);

            Vector2 buttonPos = new Vector2(0, halfSize.Y - (lblSource.Size.Y + GuiSpacing));
            MyGuiControlButton btnInfo = new MyGuiControlButton(position: new Vector2(buttonPos.X - GuiSpacing, buttonPos.Y - GuiSpacing), text: new StringBuilder("More Info"), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM, onButtonClick: OnPluginOpenClick);
            MyGuiControlButton btnSettings = new MyGuiControlButton(position: new Vector2(buttonPos.X + GuiSpacing, buttonPos.Y - GuiSpacing), text: new StringBuilder("Settings"), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM, onButtonClick: OnPluginSettingsClick);
            btnSettings.Enabled = pluginInstance != null && pluginInstance.HasConfigDialog;
            Controls.Add(btnInfo);
            Controls.Add(btnSettings);

            // Center
            MyLayoutTable layout = GetLayoutTableBetween(caption, btnInfo, verticalSpacing: GuiSpacing * 2);
            layout.SetColumnWidthsNormalized(0.5f, 0.5f);
            layout.SetRowHeightsNormalized(0.05f, 0.05f, 0.05f, 0.85f);

            layout.Add(new MyGuiControlLabel(text: plugin.FriendlyName, textScale: 0.9f), MyAlignH.Left, MyAlignV.Bottom, 0, 0);
            layout.Add(new MyGuiControlLabel(text: plugin.Author), MyAlignH.Left, MyAlignV.Top, 1, 0);
            layout.Add(new MyGuiControlLabel(text: stats.Players + " installs"), MyAlignH.Left, MyAlignV.Center, 2, 0);

            MyGuiControlCompositePanel panel = new MyGuiControlCompositePanel()
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK_BORDER
            };
            layout.AddWithSize(panel, MyAlignH.Center, MyAlignV.Center, 3, 0, colSpan: 2);
            MyGuiControlMultilineText descriptionText = new MyGuiControlMultilineText(position: panel.Position, size: panel.Size, textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {
                OriginAlign = panel.OriginAlign
            };
            descriptionText.OnLinkClicked += (x, url) => MyGuiSandbox.OpenUrl(url, UrlOpenMode.SteamOrExternalWithConfirm);
            plugin.GetDescriptionText(descriptionText);
            Controls.Add(descriptionText);

            MyGuiControlCheckbox enabledCheckbox = new MyGuiControlCheckbox(toolTip: "Enabled");
            layout.Add(enabledCheckbox, MyAlignH.Right, MyAlignV.Top, 0, 1);

            MyGuiControlParent votingPanel = new MyGuiControlParent();
            layout.AddWithSize(votingPanel, MyAlignH.Center, MyAlignV.Center, 1, 1, 2);
            CreateVotingPanel(votingPanel);

        }

        private void CreateVotingPanel(MyGuiControlParent parent)
        {
            MyLayoutHorizontal layout = new MyLayoutHorizontal(parent, 0);

            MyGuiControlButton btnVoteUp = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.SquareSmall, onButtonClick: OnRateUpClicked);
            AddImageToButton(btnVoteUp, @"Textures\GUI\Icons\Blueprints\like_test.png", 0.8f);
            layout.Add(btnVoteUp, MyAlignV.Bottom);

            MyGuiControlLabel lblVoteUp = new MyGuiControlLabel(text: stats.Upvotes.ToString());
            PositionToRight(btnVoteUp, lblVoteUp, spacing: GuiSpacing / 5);
            AdvanceLayout(ref layout, lblVoteUp.Size.X + GuiSpacing);
            parent.Controls.Add(lblVoteUp);

            MyGuiControlButton btnVoteDown = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.SquareSmall, onButtonClick: OnRateUpClicked);
            AddImageToButton(btnVoteDown, @"Textures\GUI\\Icons\Blueprints\dislike_test.png", 0.8f);
            layout.Add(btnVoteDown, MyAlignV.Bottom);

            MyGuiControlLabel lblVoteDown = new MyGuiControlLabel(text: stats.Downvotes.ToString());
            PositionToRight(btnVoteDown, lblVoteDown, spacing: GuiSpacing / 5);
            parent.Controls.Add(lblVoteDown);
        }

        private void OnRateUpClicked(MyGuiControlButton btn)
        {

        }

        private void OnPluginSettingsClick(MyGuiControlButton btn)
        {
            if (pluginInstance != null)
                pluginInstance.OpenConfig();
        }

        private void OnPluginOpenClick(MyGuiControlButton btn)
        {
            plugin.Show();
        }

        private void SettingsClick(MyGuiControlButton btn)
        {
            pluginInstance.OpenConfig();
        }

        private void MoreInfoClick(MyGuiControlButton btn)
        {
            plugin.Show();
        }
    }
}
