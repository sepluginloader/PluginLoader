using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System.Text;
using VRageMath;
using VRage.Utils;
using avaness.PluginLoader.Data;
using System.Collections.Generic;
using System;
using System.Linq;
using avaness.PluginLoader.Stats.Model;
using VRage.Game;

namespace avaness.PluginLoader.GUI
{
    public class AddPluginMenu : PluginScreen
    {
        const int ListItemsHorizontal = 2;
        const int ListItemsVertical = 3;

        private List<PluginData> plugins = new List<PluginData>();
        private PluginStats stats;
        private bool mods;

        public AddPluginMenu(IEnumerable<PluginData> plugins, bool mods) : base(size: new Vector2(1, 0.9f))
        {
            this.plugins = plugins.Where(x => (x is ModPlugin) == mods).OrderBy(x => x.FriendlyName).ToList();
            stats = Main.Instance.Stats ?? new PluginStats();
            this.mods = mods;
        }

        public override string GetFriendlyName()
        {
            return nameof(AddPluginMenu);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            // Top
            MyGuiControlLabel caption = AddCaption("Plugin List", captionScale: 1);
            AddBarBelow(caption);

            // Bottom
            Vector2 bottomMid = new Vector2(0, m_size.Value.Y / 2);
            MyGuiControlButton btnApply = new MyGuiControlButton(position: new Vector2(bottomMid.X - GuiSpacing, bottomMid.Y - GuiSpacing), text: new StringBuilder("Apply"), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            MyGuiControlButton btnCancel = new MyGuiControlButton(position: new Vector2(bottomMid.X + GuiSpacing, bottomMid.Y - GuiSpacing), text: new StringBuilder("Cancel"), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            Controls.Add(btnApply);
            Controls.Add(btnCancel);
            AddBarAbove(btnApply);

            // Center 
            Vector2 halfSize = m_size.Value / 2;
            Vector2 searchPos = new Vector2(GuiSpacing - halfSize.X, GetCoordTopLeftFromAligned(caption).Y + caption.Size.Y + (GuiSpacing * 2));
            MyGuiControlSearchBox searchBox = new MyGuiControlSearchBox(position: searchPos, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            searchBox.Size = new Vector2(m_size.Value.X - (GuiSpacing * 2), searchBox.Size.Y);
            Controls.Add(searchBox);

            RectangleF area = GetAreaBetween(searchBox, btnApply, GuiSpacing * 2);

            MyGuiControlParent gridArea = new MyGuiControlParent(position: area.Position)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };
            MyGuiControlScrollablePanel scrollPanel = new MyGuiControlScrollablePanel(gridArea)
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                BorderHighlightEnabled = true,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = area.Position,
                Size = area.Size,
                ScrollbarVEnabled = true,
                CanFocusChildren = true,
                ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
                DrawScrollBarSeparator = true,
            };
            gridArea.Position = area.Position;
            gridArea.Size = area.Size - (scrollPanel.ScrolledAreaPadding.SizeChange + new Vector2(scrollPanel.ScrollbarVSizeX, 0));
            CreatePluginList(gridArea);
            Controls.Add(scrollPanel);
        }

        private void CreatePluginList(MyGuiControlParent panel)
        {
            Vector2 itemSize = panel.Size / new Vector2(ListItemsHorizontal, ListItemsVertical);
            int totalRows = (int)Math.Ceiling(plugins.Count / (float)ListItemsHorizontal);
            panel.Size = new Vector2(panel.Size.X, itemSize.Y * totalRows);

            Vector2 itemPositionOffset = (itemSize / 2) - (panel.Size / 2);

            for (int i = 0; i < plugins.Count; i++)
            {
                PluginData plugin = plugins[i];
                
                int row = i / ListItemsHorizontal;
                int col = i % ListItemsHorizontal;
                Vector2 itemPosition = (itemSize * new Vector2(col, row)) + itemPositionOffset;
                MyGuiControlParent itemPanel = new MyGuiControlParent(position: itemPosition, size: itemSize);
                CreatePluginListItem(plugin, itemPanel);
                panel.Controls.Add(itemPanel);
            }
        }

        private void CreatePluginListItem(PluginData plugin, MyGuiControlParent panel)
        {
            float padding = GuiSpacing;

            MyGuiControlParent contentArea = new MyGuiControlParent(size: panel.Size - padding)
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK,
            };

            Vector2 contentTopLeft = GetCoordTopLeftFromAligned(contentArea) + padding;
            Vector2 contentSize = contentArea.Size - (padding * 2);

            MyLayoutTable layout = new MyLayoutTable(contentArea, contentTopLeft, contentSize);
            layout.SetColumnWidthsNormalized(0.5f, 0.5f);
            layout.SetRowHeightsNormalized(0.1f, 0.1f, 0.6f, 0.1f, 0.1f);

            layout.Add(new MyGuiControlLabel(text: plugin.FriendlyName), MyAlignH.Left, MyAlignV.Bottom, 0, 0);
            if(!plugin.IsLocal)
            {
                layout.Add(new MyGuiControlLabel(text: plugin.Author), MyAlignH.Left, MyAlignV.Top, 1, 0);
                
                MyGuiControlMultilineText description = new MyGuiControlMultilineText(textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
                {
                    VisualStyle = MyGuiControlMultilineStyleEnum.NeutralBordered,
                    BorderEnabled = true,
                    Visible = true
                };
                layout.AddWithSize(description, MyAlignH.Left, MyAlignV.Top, 2, 0, 1, 2);
                if (!string.IsNullOrEmpty(plugin.Tooltip))
                    description.AppendText(plugin.Tooltip);


                PluginStat stat = stats.GetStatsForPlugin(plugin);
                layout.Add(new MyGuiControlLabel(text: stat.Players + " installs"), MyAlignH.Left, MyAlignV.Bottom, 3, 0);

                MyGuiControlParent votingPanel = new MyGuiControlParent();
                layout.AddWithSize(votingPanel, MyAlignH.Center, MyAlignV.Center, 3, 1, 2);
                CreateVotingPanel(votingPanel, stat);

            }
            layout.Add(new MyGuiControlLabel(text: plugin.Source), MyAlignH.Left, MyAlignV.Bottom, 4, 0);

            panel.Controls.Add(contentArea);
        }

        private void CreateVotingPanel(MyGuiControlParent parent, PluginStat stats)
        {
            MyLayoutHorizontal layout = new MyLayoutHorizontal(parent, 0);

            float height = parent.Size.Y;
            float width = (height * MyGuiConstants.GUI_OPTIMAL_SIZE.Y) / MyGuiConstants.GUI_OPTIMAL_SIZE.X;
            Vector2 size = new Vector2(width, height) * 0.8f;

            MyGuiControlImage imgVoteUp = new MyGuiControlImage(size: size, textures: new[] { @"Textures\GUI\Icons\Blueprints\like_test.png" });
            layout.Add(imgVoteUp, MyAlignV.Center);

            MyGuiControlLabel lblVoteUp = new MyGuiControlLabel(text: stats.Upvotes.ToString());
            PositionToRight(imgVoteUp, lblVoteUp, spacing: GuiSpacing / 5);
            AdvanceLayout(ref layout, lblVoteUp.Size.X + GuiSpacing);
            parent.Controls.Add(lblVoteUp);

            MyGuiControlImage imgVoteDown = new MyGuiControlImage(size: size, textures: new[] { @"Textures\GUI\Icons\Blueprints\dislike_test.png" });
            layout.Add(imgVoteDown, MyAlignV.Center);

            MyGuiControlLabel lblVoteDown = new MyGuiControlLabel(text: stats.Downvotes.ToString());
            PositionToRight(imgVoteDown, lblVoteDown, spacing: GuiSpacing / 5);
            parent.Controls.Add(lblVoteDown);
        }
    }
}
