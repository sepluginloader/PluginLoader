using System;
using avaness.PluginLoader.Data;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI.GuiControls
{
    public class PluginDetailsPanel : MyGuiControlParent
    {
        public event Action<PluginData, bool> OnPluginToggled;

        // Amount of stars
        private const int MaxRating = 10;

        // Panel controls
        private MyGuiControlLabel pluginNameLabel;
        private MyGuiControlLabel pluginNameText;
        private MyGuiControlLabel authorLabel;
        private MyGuiControlLabel authorText;
        private MyGuiControlLabel versionLabel;
        private MyGuiControlLabel versionText;
        private MyGuiControlLabel statusLabel;
        private MyGuiControlLabel statusText;
        private MyGuiControlLabel usageLabel;
        private MyGuiControlLabel usageText;
        private MyGuiControlLabel ratingLabel;
        private RatingControl ratingControl;
        private MyGuiControlButton upvoteButton;
        private MyGuiControlImage upVoteIcon;
        private MyGuiControlButton downvoteButton;
        private MyGuiControlImage downVoteIcon;
        private MyGuiControlMultilineText descriptionText;
        private MyGuiControlCompositePanel descriptionPanel;
        private MyGuiControlLabel enableLabel;
        private MyGuiControlCheckbox enableCheckbox;
        private MyGuiControlButton infoButton;

        // Layout management
        private MyLayoutTable layoutTable;

        // Plugin currently loaded into the panel or null if none are loaded
        private PluginData plugin;

        public PluginData Plugin
        {
            get => plugin;
            set
            {
                if (ReferenceEquals(value, Plugin))
                    return;

                plugin = value;

                if (plugin == null)
                {
                    DisableControls();
                    ClearPluginData();
                    return;
                }

                EnableControls();
                LoadPluginData();
            }
        }

        private void DisableControls()
        {
            foreach (var control in Controls)
                control.Enabled = false;
        }

        private void EnableControls()
        {
            foreach (var control in Controls)
                control.Enabled = true;
        }

        private void ClearPluginData()
        {
            pluginNameText.Text = "";
            authorText.Text = "";
            versionText.Text = "";
            statusText.Text = "";
            usageText.Text = "";
            ratingControl.Value = 0;
            upvoteButton.Checked = false;
            downvoteButton.Checked = false;
            descriptionText.Text.Clear();
            enableCheckbox.IsChecked = false;
        }

        public void LoadPluginData()
        {
            pluginNameText.Text = Plugin.FriendlyName ?? "N/A";
            authorText.Text = Plugin.Author ?? "N/A";
            versionText.Text = Plugin.Version?.ToString() ?? "N/A";
            statusText.Text = Plugin.Status == PluginStatus.None ? "Up to date" : Plugin.StatusString;
            usageText.Text = "N/A"; // TODO: Get from plugin stats
            ratingControl.Value = 5; // TODO: Get from plugin stats
            upvoteButton.Checked = false; // TODO: Get from plugin stats
            downvoteButton.Checked = false; // TODO: Get from plugin stats
            descriptionText.Text.Clear().Append(Plugin.Tooltip ?? "N/A"); // TODO: Extend the XML with description
            enableCheckbox.IsChecked = Plugin.EnableAfterRestart;
        }

        public virtual void CreateControls(Vector2 rightSideOrigin)
        {
            // Plugin name
            pluginNameLabel = new MyGuiControlLabel
            {
                Text = "Plugin Name",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            pluginNameText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Author
            authorLabel = new MyGuiControlLabel
            {
                Text = "Author",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            authorText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Version
            versionLabel = new MyGuiControlLabel
            {
                Text = "Version",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            versionText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Status
            statusLabel = new MyGuiControlLabel
            {
                Text = "Status",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            statusText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Usage
            usageLabel = new MyGuiControlLabel
            {
                Text = "Usage",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            usageText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Rating
            ratingLabel = new MyGuiControlLabel
            {
                Text = "Rating",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            ratingControl = new RatingControl(0, MaxRating)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Voting buttons
            upvoteButton = CreateRateButton(true);
            upVoteIcon = CreateRateIcon(upvoteButton, "Textures\\GUI\\Icons\\Blueprints\\like_test.png");
            downvoteButton = CreateRateButton(false);
            downVoteIcon = CreateRateIcon(downvoteButton, "Textures\\GUI\\Icons\\Blueprints\\dislike_test.png");

            // Plugin description
            descriptionText = new MyGuiControlMultilineText(null)
            {
                Name = "DescriptionText",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };
            descriptionPanel = new MyGuiControlCompositePanel
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK_BORDER
            };

            // Enable checkbox
            enableLabel = new MyGuiControlLabel
            {
                Text = "Enabled",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            enableCheckbox = new MyGuiControlCheckbox(toolTip: "Enables loading the plugin when SE is started.")
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Enabled = false
            };
            enableCheckbox.IsCheckedChanged += TogglePlugin;

            // Info button
            infoButton = new MyGuiControlButton(onButtonClick: _ => Plugin?.Show())
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Text = "Plugin Info"
            };

            LayoutControls(rightSideOrigin);
        }

        private void LayoutControls(Vector2 rightSideOrigin)
        {
            layoutTable = new MyLayoutTable(this, rightSideOrigin, new Vector2(1f, 1f));
            layoutTable.SetColumnWidths(218f, 418f);
            layoutTable.SetRowHeights(75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f);

            var row = 0;

            layoutTable.Add(pluginNameLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(pluginNameText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(authorLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(authorText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(versionLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(versionText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(statusLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(statusText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(usageLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(usageText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(ratingLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(ratingControl, MyAlignH.Left, MyAlignV.Center, row, 1);
            layoutTable.Add(upvoteButton, MyAlignH.Right, MyAlignV.Center, row, 1);
            layoutTable.Add(upVoteIcon, MyAlignH.Center, MyAlignV.Center, row, 1);
            layoutTable.Add(downvoteButton, MyAlignH.Right, MyAlignV.Center, row, 1);
            layoutTable.Add(downVoteIcon, MyAlignH.Center, MyAlignV.Center, row, 1);
            upvoteButton.PositionX -= 0.07f;
            downvoteButton.PositionX -= 0.02f;
            upVoteIcon.Position = upvoteButton.Position + new Vector2(-0.0015f, -0.002f) - new Vector2(upvoteButton.Size.X / 2f, 0f);
            downVoteIcon.Position = downvoteButton.Position + new Vector2(-0.0015f, -0.002f) - new Vector2(downvoteButton.Size.X / 2f, 0f);
            row++;

            descriptionPanel.Size += new Vector2(0.01f, 0f);
            layoutTable.AddWithSize(descriptionPanel, MyAlignH.Center, MyAlignV.Top, row, 0, 4, 2);
            layoutTable.AddWithSize(descriptionText, MyAlignH.Left, MyAlignV.Bottom, row, 0, 4, 2);
            row += 4;

            layoutTable.Add(enableLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(enableCheckbox, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.AddWithSize(infoButton, MyAlignH.Right, MyAlignV.Center, row, 0, 1, colSpan:2);
            // row++;

            DisableControls();
        }

        private void TogglePlugin(MyGuiControlCheckbox obj)
        {
            if (plugin == null)
                return;

            OnPluginToggled?.Invoke(plugin, enableCheckbox.IsChecked);
        }

        // From Sandbox.Game.Screens.MyGuiScreenNewWorkshopGame

        #region Vote buttons

        private MyGuiControlButton CreateRateButton(bool positive)
        {
            return new MyGuiControlButton(null, MyGuiControlButtonStyleEnum.Rectangular, onButtonClick: positive ? OnRateUpClicked : new Action<MyGuiControlButton>(OnRateDownClicked), size: new Vector2(0.03f));
        }

        private MyGuiControlImage CreateRateIcon(MyGuiControlButton button, string texture)
        {
            MyGuiControlImage myGuiControlImage = new MyGuiControlImage(null, null, null, null, new[] { texture });
            AdjustButtonForIcon(button, myGuiControlImage);
            myGuiControlImage.Size = button.Size * 0.6f;
            return myGuiControlImage;
        }

        private void AdjustButtonForIcon(MyGuiControlButton button, MyGuiControlImage icon)
        {
            button.Size = new Vector2(button.Size.X, button.Size.X * 4f / 3f);
            button.HighlightChanged += delegate(MyGuiControlBase x) { icon.ColorMask = (x.HasHighlight ? MyGuiConstants.HIGHLIGHT_TEXT_COLOR : Vector4.One); };
        }

        #endregion

        private void OnRateUpClicked(MyGuiControlButton button)
        {
            // TODO: Submit vote to plugin stats, update from response
        }

        private void OnRateDownClicked(MyGuiControlButton button)
        {
            // TODO: Submit vote to plugin stats, update from response
        }
    }
}