using avaness.PluginLoader.Data;
using avaness.PluginLoader.GUI.GuiControls;
using Sandbox;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using static avaness.PluginLoader.Data.PluginData;
using static Sandbox.Graphics.GUI.MyGuiScreenMessageBox;

namespace avaness.PluginLoader.GUI
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
        private Vector2 rightSideOrigin;
        private const float BarWidth = 0.85f;
        private const float Spacing = 0.0175f;

        //Amount of stars
        private const int MaxRating = 9;

        private readonly Dictionary<string, bool> dataChanges = new Dictionary<string, bool>();
        private readonly Dictionary<string, MyGuiControlCheckbox> dataCheckboxes = new Dictionary<string, MyGuiControlCheckbox>();

        private MyGuiControlTable pluginTable;
        private MyGuiControlLabel pluginCountLabel;

        private static PluginConfig Config => Main.Instance.Config;
        private string[] tableFilter;
        private PluginData selectedPlugin;

        private static bool allItemsVisible = true;

        #region Right side Controls
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
        private RatingControl ratingDisplay;
        private MyGuiControlButton buttonRateUp;
        private MyGuiControlImage iconRateUp;
        private MyGuiControlButton buttonRateDown;
        private MyGuiControlImage iconRateDown;
        private MyGuiControlMultilineText descriptionText;
        private MyGuiControlCompositePanel descriptionPanel;
        private MyGuiControlLabel toggleButtonLabel;
        private MyGuiControlCheckbox toggleButton;
        private MyGuiControlButton infoButton;
        #endregion

        #region Icons
        // Source: MyTerminalControlPanel
        private static MyGuiHighlightTexture IconHide = new MyGuiHighlightTexture
        {
            Normal = "Textures\\GUI\\Controls\\button_hide.dds",
            Highlight = "Textures\\GUI\\Controls\\button_hide.dds",
            Focus = "Textures\\GUI\\Controls\\button_hide_focus.dds",
            SizePx = new Vector2(40f, 40f)
        };

        // Source: MyTerminalControlPanel
        private static MyGuiHighlightTexture IconShow = new MyGuiHighlightTexture
        {
            Normal = "Textures\\GUI\\Controls\\button_unhide.dds",
            Highlight = "Textures\\GUI\\Controls\\button_unhide.dds",
            Focus = "Textures\\GUI\\Controls\\button_unhide_focus.dds",
            SizePx = new Vector2(40f, 40f)
        };
        private MyLayoutTable layoutTable;
        #endregion

        /// <summary>
        /// The plugins screen, the contructor itself sets up the menu properties.
        /// </summary>
        public MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(1f, 0.97f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
        {
            EnabledBackgroundFade = true;
            m_closeOnEsc = true;
            m_drawEvenWithoutFocus = true;
            CanHideOthers = true;
            CanBeHidden = true;
            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenPluginConfig";
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        /// <summary>
        /// Initializes the controls of the menu on the left side of the menu.
        /// </summary>
        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            MyGuiControlLabel title = AddCaption("Plugins List");

            // Sets the origin relative to the center of the caption on the X axis and to the bottom the caption on the y axis.
            Vector2 origin = title.Position += new Vector2(0f, title.Size.Y / 2);

            origin.Y += Spacing;

            // Adds a bar right below the caption.
            MyGuiControlSeparatorList titleBar = new MyGuiControlSeparatorList();
            titleBar.AddHorizontal(new Vector2(origin.X - (BarWidth / 2), origin.Y), BarWidth);
            Controls.Add(titleBar);

            origin.Y += Spacing;

            // Change the position of this to move the entire middle section of the menu, the menu bars, menu title, and bottom buttons won't move
            // Adds a search bar right below the bar on the left side of the menu.
            MyGuiControlSearchBox searchBox = new MyGuiControlSearchBox(new Vector2(origin.X - (BarWidth / 2), origin.Y), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);

            // Changing the search box X size will change the plugin list length.
            searchBox.Size = new Vector2(0.4f, searchBox.Size.Y);
            searchBox.OnTextChanged += SearchBox_TextChanged;
            Controls.Add(searchBox);

            #region Visibility Button
            // Adds a button to show only enabled plugins. Located right of the search bar.
            MyGuiControlButton buttonVisibility = new MyGuiControlButton(new Vector2(origin.X - (BarWidth / 2) + searchBox.Size.X, origin.Y) + new Vector2(0.003f, 0.002f), MyGuiControlButtonStyleEnum.Rectangular, new Vector2(searchBox.Size.Y * 2.52929769833f), onButtonClick: OnVisibilityClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, toolTip: "Show only enabled plugins.", buttonScale: 0.5f);

            if (allItemsVisible || Config.Count == 0)
            {
                allItemsVisible = true;
                buttonVisibility.Icon = IconHide;
            }
            else
            {
                buttonVisibility.Icon = IconShow;
            }

            Controls.Add(buttonVisibility);
            rightSideOrigin = buttonVisibility.Position + new Vector2(Spacing * 1.778f + (buttonVisibility.Size.X / 2), -(buttonVisibility.Size.Y / 2));
            #endregion

            origin.Y += searchBox.Size.Y + Spacing;

            #region Plugin List
            // Adds the plugin list on the right of the menu below the search bar.
            pluginTable = new MyGuiControlTable
            {
                Position = new Vector2(origin.X - (BarWidth / 2), origin.Y),
                Size = new Vector2(searchBox.Size.X + buttonVisibility.Size.X + 0.001f, 0.6f), //The y value can be bigger than the visible rows count as the visibleRowsCount controls the height.
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                ColumnsCount = 3,
                VisibleRowsCount = 20
            };

            pluginTable.SetCustomColumnWidths(new[]
            {
                0.22f,
                0.6f,
                0.22f
            });

            pluginTable.SetColumnName(0, new StringBuilder("Source"));
            pluginTable.SetColumnComparison(0, CellTextOrDataComparison);
            pluginTable.SetColumnName(1, new StringBuilder("Name"));
            pluginTable.SetColumnComparison(1, CellTextComparison);
            pluginTable.SetColumnName(2, new StringBuilder());
            pluginTable.SetColumnComparison(2, CellTextComparison);

            // Sorts the plugin table by the name of the plugin.
            pluginTable.SortByColumn(1);

            // Selecting list items load their details in OnItemSelected
            pluginTable.ItemSelected += OnItemSelected;
            Controls.Add(pluginTable);
            #endregion

            origin.Y += Spacing + pluginTable.Size.Y;

            // Adds the bar at the bottom between just above the buttons.
            MyGuiControlSeparatorList bottomBar = new MyGuiControlSeparatorList();
            bottomBar.AddHorizontal(new Vector2(origin.X - (BarWidth / 2), origin.Y), BarWidth);
            Controls.Add(bottomBar);

            origin.Y += Spacing;

            // Adds buttons at bottom of menu
            MyGuiControlButton buttonRestart = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Restart the game and apply changes.", new StringBuilder("Apply"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnRestartButtonClick);

            MyGuiControlButton buttonClose = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, null, new StringBuilder("Cancel"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnCloseButtonClick);

            AlignRow(origin, 0.02f, buttonRestart, buttonClose);
            Controls.Add(buttonRestart);
            Controls.Add(buttonClose);

            // Adds a place to show the total amount of plugins and to show the total amount of visible plugins.
            pluginCountLabel = new MyGuiControlLabel(new Vector2(origin.X - (BarWidth / 2), buttonRestart.Position.Y), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            Controls.Add(pluginCountLabel);

            // Refreshes the table to show plugins on plugin list
            RefreshTable();
        }

        /// <summary>
        /// Refreshes the right side controls. The right side controls always stay next to the plugin list control even if you move the plugin list control.
        /// </summary>
        /// <param name="data">Plugin data to show on right.</param>
        private void RefreshRight(PluginData data)
        {
            #region Hide existing controls
            // Hides existing controls so the old and new won't overlap. There is no way to remove the existing controls. OnRemoving just sets the controls in their default position.
            if (pluginNameLabel != null)
                pluginNameLabel.Visible = false;
            if (pluginNameText != null)
                pluginNameText.Visible = false;
            if (authorLabel != null)
                authorLabel.Visible = false;
            if (authorText != null)
                authorText.Visible = false;
            if (versionLabel != null)
                versionLabel.Visible = false;
            if (versionText != null)
                versionText.Visible = false;
            if (statusLabel != null)
                statusLabel.Visible = false;
            if (statusText != null)
                statusText.Visible = false;
            if (usageLabel != null)
                usageLabel.Visible = false;
            if (usageText != null)
                usageText.Visible = false;
            if (ratingLabel != null)
                ratingLabel.Visible = false;
            if (ratingDisplay != null)
                ratingDisplay.Visible = false;
            if (buttonRateUp != null)
                buttonRateUp.Visible = false;
            if (iconRateUp != null)
                iconRateUp.Visible = false;
            if (buttonRateDown != null)
                buttonRateDown.Visible = false;
            if (iconRateDown != null)
                iconRateDown.Visible = false;
            if (descriptionText != null)
                descriptionText.Visible = false;
            if (descriptionPanel != null)
                descriptionPanel.Visible = false;
            if (toggleButtonLabel != null)
                toggleButtonLabel.Visible = false;
            if (toggleButton != null)
                toggleButton.Visible = false;
            if (infoButton != null)
                infoButton.Visible = false;
            #endregion

            selectedPlugin = data;
            layoutTable = new MyLayoutTable(this, rightSideOrigin, new Vector2(1f, 1f));
            layoutTable.SetColumnWidths(318f, 318f);
            layoutTable.SetRowHeights(75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f);

            #region Right side controls
            //Plugin Name
            pluginNameLabel = new MyGuiControlLabel
            {
                Text = "Plugin Name:",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            pluginNameText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            //Author
            authorLabel = new MyGuiControlLabel
            {
                Text = "Author:",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            authorText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            //Version
            versionLabel = new MyGuiControlLabel
            {
                Text = "Version:",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            versionText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            //Status
            statusLabel = new MyGuiControlLabel
            {
                Text = "Status:",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            statusText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            //Usage
            usageLabel = new MyGuiControlLabel
            {
                Text = "Usage:",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            usageText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            //Rating
            ratingLabel = new MyGuiControlLabel
            {
                Text = "Rating:",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            ratingDisplay = new RatingControl(10)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            //Rate up
            buttonRateUp = CreateRateButton(positive: true);
            iconRateUp = CreateRateIcon(buttonRateUp, "Textures\\GUI\\Icons\\Blueprints\\like_test.png");

            //Rate down
            buttonRateDown = CreateRateButton(positive: false);
            iconRateDown = CreateRateIcon(buttonRateDown, "Textures\\GUI\\Icons\\Blueprints\\dislike_test.png");

            //Description
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

            //Plugin On/Off
            toggleButtonLabel = new MyGuiControlLabel
            {
                Text = "On/Off",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            toggleButton = new MyGuiControlCheckbox(toolTip: "Enables or disables the plugin.")
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Enabled = false
            };

            //Info button
            infoButton = new MyGuiControlButton(onButtonClick: OnInfoButtonClick)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Text = "Plugin Info"
            };
            #endregion

            #region Table left
            // Left side of table.
            layoutTable.Add(pluginNameLabel, MyAlignH.Left, MyAlignV.Center, 0, 0);
            layoutTable.Add(authorLabel, MyAlignH.Left, MyAlignV.Center, 1, 0);

            int rowLeft;

            if (data.Version != null && data.Status != PluginStatus.None)
            {
                layoutTable.Add(versionLabel, MyAlignH.Left, MyAlignV.Center, 2, 0);
                layoutTable.Add(statusLabel, MyAlignH.Left, MyAlignV.Center, 3, 0);
                rowLeft = 4;
            }
            else if (data.Status == PluginStatus.None && data.Version == null)
            {
                rowLeft = 2;
            }
            else
            {
                layoutTable.Add(versionLabel, MyAlignH.Left, MyAlignV.Center, 2, 0);
                rowLeft = 3;
            }

            layoutTable.Add(usageLabel, MyAlignH.Left, MyAlignV.Center, rowLeft, 0);
            ++rowLeft;
            layoutTable.Add(ratingLabel, MyAlignH.Left, MyAlignV.Center, rowLeft, 0);
            ++rowLeft;
            layoutTable.AddWithSize(descriptionPanel, MyAlignH.Center, MyAlignV.Top, rowLeft, 0, 3, 2);

            descriptionPanel.Size += new Vector2(0.01f, 0f);

            layoutTable.AddWithSize(descriptionText, MyAlignH.Left, MyAlignV.Bottom, rowLeft, 0, 3, 2);
            rowLeft += 3;
            layoutTable.Add(toggleButtonLabel, MyAlignH.Left, MyAlignV.Top, rowLeft, 0);
            ++rowLeft;
            layoutTable.Add(infoButton, MyAlignH.Left, MyAlignV.Top, rowLeft, 0);
            #endregion

            #region Table Right
            // Right side of table.
            layoutTable.Add(pluginNameText, MyAlignH.Left, MyAlignV.Center, 0, 1);
            layoutTable.Add(authorText, MyAlignH.Left, MyAlignV.Center, 1, 1);

            int rowRight;

            if (data.Version != null && data.Status != PluginStatus.None)
            {
                layoutTable.Add(versionText, MyAlignH.Left, MyAlignV.Center, 2, 1);
                layoutTable.Add(statusText, MyAlignH.Left, MyAlignV.Center, 3, 1);
                rowRight = 4;
            }
            else if (data.Status == PluginStatus.None && data.Version == null)
            {
                rowRight = 2;
            }
            else
            {
                layoutTable.Add(versionText, MyAlignH.Left, MyAlignV.Center, 2, 1);
                rowRight = 3;
            }

            layoutTable.Add(usageText, MyAlignH.Left, MyAlignV.Center, rowRight, 1);
            ++rowRight;
            layoutTable.Add(ratingDisplay, MyAlignH.Left, MyAlignV.Center, rowRight, 1);
            ratingDisplay.MaxValue = MaxRating;
            layoutTable.Add(buttonRateUp, MyAlignH.Right, MyAlignV.Center, rowRight, 1);
            layoutTable.Add(iconRateUp, MyAlignH.Center, MyAlignV.Center, rowRight, 1);
            layoutTable.Add(buttonRateDown, MyAlignH.Right, MyAlignV.Center, rowRight, 1);
            layoutTable.Add(iconRateDown, MyAlignH.Center, MyAlignV.Center, rowRight, 1);
            rowRight += 4;
            layoutTable.Add(toggleButton, MyAlignH.Left, MyAlignV.Top, rowRight, 1);
            buttonRateUp.PositionX -= 0.05f;
            iconRateUp.Position = buttonRateUp.Position + new Vector2(-0.0015f, -0.002f) - new Vector2(buttonRateUp.Size.X / 2f, 0f);
            iconRateDown.Position = buttonRateDown.Position + new Vector2(-0.0015f, -0.002f) - new Vector2(buttonRateDown.Size.X / 2f, 0f);
            #endregion

            RefreshPluginStats();
        }

        private void RefreshPluginStats()
        {
            ratingDisplay.Value = selectedPlugin.Rating;
            RateStatus rateStatus = selectedPlugin.GetRateStatus();
            if (rateStatus == RateStatus.RatedUp)
            {
                buttonRateUp.Checked = true;
            }
            else if (rateStatus == RateStatus.RatedDown)
            {
                buttonRateDown.Checked = true;
            }
        }

        /// <summary>
        /// Event that triggers when the visibility button is clicked. This method shows all plugins or only enabled plugins.
        /// </summary>
        /// <param name="btn">The button to assign this event to.</param>
        private void OnVisibilityClick(MyGuiControlButton btn)
        {
            if (allItemsVisible)
            {
                allItemsVisible = false;
                btn.Icon = IconShow;
            }
            else
            {
                allItemsVisible = true;
                btn.Icon = IconHide;
            }
            RefreshTable(tableFilter);
        }

        private int CellTextOrDataComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            int result = TextComparison(x.Text, y.Text);
            if (result != 0)
            {
                return result;
            }

            return TextComparison((StringBuilder)x.UserData, (StringBuilder)y.UserData);
        }

        private int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            return TextComparison(x.Text, y.Text);
        }

        private int TextComparison(StringBuilder x, StringBuilder y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                return 1;
            }

            if (y == null)
                return -1;

            return x.CompareTo(y);
        }

        /// <summary>
        /// Clears the table and adds the list of plugins and their infomation.
        /// </summary>
        /// <param name="filter">Text filter</param>
        private void RefreshTable(string[] filter = null)
        {
            pluginTable.Clear();
            pluginTable.Controls.Clear();
            dataCheckboxes.Clear();
            PluginList list = Main.Instance.List;
            bool noFilter = filter == null || filter.Length == 0;
            foreach (PluginData data in list)
            {
                if (!dataChanges.TryGetValue(data.Id, out bool enabled))
                    enabled = Config.IsEnabled(data.Id);

                if (noFilter && (data.Hidden || !allItemsVisible) && !enabled)
                    continue;

                if (noFilter || FilterName(data.FriendlyName, filter))
                {
                    MyGuiControlTable.Row row = new MyGuiControlTable.Row(data);
                    pluginTable.Add(row);

                    StringBuilder name = new StringBuilder(data.FriendlyName);

                    row.AddCell(new MyGuiControlTable.Cell(data.Source, name));

                    string tip = data.FriendlyName;
                    if (!string.IsNullOrWhiteSpace(data.Tooltip))
                    {
                        tip += "\n" + data.Tooltip;
                    }

                    row.AddCell(new MyGuiControlTable.Cell(data.FriendlyName, toolTip: tip));

                    string enabledText;
                    if (enabled)
                    {
                        enabledText = "Enabled";
                    }
                    else
                    {
                        enabledText = "Disabled";
                    }
                    row.AddCell(new MyGuiControlTable.Cell(enabledText));
                }
            }
            pluginCountLabel.Text = pluginTable.RowsCount + " out of the total " + list.Count + " \nplugins are visible.";
            pluginTable.Sort(false);
            pluginTable.SelectedRowIndex = null;
            tableFilter = filter;
            pluginTable.SelectedRowIndex = 0;
            MyGuiControlTable.EventArgs args = new MyGuiControlTable.EventArgs();
            args.RowIndex = 0;
            OnItemSelected(pluginTable, args);
        }

        /// <summary>
        /// Event that triggers when the text in the searchbox is changed.
        /// </summary>
        /// <param name="txt">The text that was entered into the searchbox.</param>
        private void SearchBox_TextChanged(string txt)
        {
            string[] args = txt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            RefreshTable(args);
        }

        private static bool FilterName(string name, IEnumerable<string> filter)
        {
            return filter.All(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sets text on right side of screen.
        /// </summary>
        /// <param name="table">Table to get the plugin data.</param>
        /// <param name="args">Event arguments.</param>
        private void OnItemSelected(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            if (!TryGetRowData(args.RowIndex, out var data))
                return;

            RefreshRight(data);

            pluginNameText.Text = data.FriendlyName;
            authorText.Text = data.Author ?? "Name is not available";
            versionText.Text = data.Version?.ToString() ?? "";
            statusText.Text = data.Status == PluginStatus.None ? "" : data.StatusString;
            descriptionText.Text = new StringBuilder(data.Tooltip ?? "Description is not available");

            toggleButton.UserData = data;
            toggleButton.IsCheckedChanged += IsCheckedChanged;

            if (!dataChanges.TryGetValue(data.Id, out bool enabled))
                enabled = Config.IsEnabled(data.Id);

            toggleButton.Enabled = true;
            toggleButton.IsChecked = enabled;
        }

        private bool TryGetRowData(int rowIndex, out PluginData pluginData)
        {
            pluginData = null;

            if (rowIndex < 0 || rowIndex >= pluginTable.RowsCount)
                return false;

            var row = pluginTable.GetRow(rowIndex);
            if (!(row.UserData is PluginData data))
                return false;

            pluginData = data;
            return true;
        }

        private void AlignRow(Vector2 origin, float spacing, params MyGuiControlBase[] elements)
        {
            if (elements.Length == 0)
                return;

            float totalWidth = 0;
            for (int i = 0; i < elements.Length; i++)
            {
                MyGuiControlBase btn = elements[i];
                totalWidth += btn.Size.X;
                if (i < elements.Length - 1)
                    totalWidth += spacing;
            }

            float originX = origin.X - (totalWidth / 2);
            foreach (MyGuiControlBase btn in elements)
            {
                float halfWidth = btn.Size.X / 2;
                originX += halfWidth;
                btn.Position = new Vector2(originX, origin.Y);
                originX += spacing + halfWidth;
            }
        }

        private void IsCheckedChanged(MyGuiControlCheckbox checkbox)
        {
            PluginData original = (PluginData)checkbox.UserData;
            SetEnabled(original, checkbox.IsChecked);
            if (original.Group.Count > 0 && checkbox.IsChecked)
            {
                foreach (PluginData alt in original.Group)
                {
                    if (SetEnabled(alt, false) && dataCheckboxes.TryGetValue(alt.Id, out MyGuiControlCheckbox altBox))
                    {
                        altBox.IsCheckedChanged -= IsCheckedChanged;
                        altBox.IsChecked = false;
                        altBox.IsCheckedChanged += IsCheckedChanged;
                    }
                }
            }
        }

        private bool SetEnabled(PluginData original, bool enabled)
        {
            if (Config.IsEnabled(original.Id) == enabled)
            {
                bool result = dataChanges.Remove(original.Id);
                ChangePluginEnableStatus(original, enabled);
                return result;
            }
            else
            {
                dataChanges[original.Id] = enabled;
                ChangePluginEnableStatus(original, enabled);
                return true;
            }
        }

        private void ChangePluginEnableStatus(PluginData plugin, bool enabled)
        {
            MyGuiControlTable.Row row = pluginTable.Find(x => ReferenceEquals(x.UserData, plugin));
            if (row == null)
                return;

            MyGuiControlTable.Cell enabledCell = row.GetCell(2);

            if (enabled && enabledCell.Text != new StringBuilder("Enabled"))
            {
                enabledCell.Text.Clear().Append("Enabled");
            }
            else if (enabledCell.Text != new StringBuilder("Disabled"))
            {
                enabledCell.Text.Clear().Append("Disabled");
            }
        }

        private void OnInfoButtonClick(MyGuiControlButton btn)
        {
            if (pluginTable.SelectedRowIndex.HasValue)
            {
                PluginData data = pluginTable.SelectedRow.UserData as PluginData;
                if (data != null)
                {
                    data.Show();
                }
            }
        }

        private void OnCloseButtonClick(MyGuiControlButton btn)
        {
            dataChanges.Clear();
            CloseScreen();
        }

        private void OnRestartButtonClick(MyGuiControlButton btn)
        {
            if (dataChanges.Count == 0)
            {
                CloseScreen();
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, MyMessageBoxButtonsType.YES_NO_CANCEL, new StringBuilder("A restart is required to apply changes. Would you like to restart the game now?"), new StringBuilder("Apply Changes?"), callback: AskRestartResult));
            }
        }

        private void Save()
        {
            if (dataChanges.Count <= 0)
                return;

            foreach (KeyValuePair<string, bool> kv in dataChanges)
            {
                Config.SetEnabled(kv.Key, kv.Value);
            }

            Config.Save();
            dataChanges.Clear();
        }

        #region RateButtons
        // From Sandbox.Game.Screens.MyGuiScreenNewWorkshopGame
        private MyGuiControlButton CreateRateButton(bool positive)
        {
            return new MyGuiControlButton(null, MyGuiControlButtonStyleEnum.Rectangular, onButtonClick: positive ? OnRateUpClicked : new Action<MyGuiControlButton>(OnRateDownClicked), size: new Vector2(0.03f));
        }

        // From Sandbox.Game.Screens.MyGuiScreenNewWorkshopGamesp
        private MyGuiControlImage CreateRateIcon(MyGuiControlButton button, string texture)
        {
            MyGuiControlImage myGuiControlImage = new MyGuiControlImage(null, null, null, null, new[] { texture });
            AdjustButtonForIcon(button, myGuiControlImage);
            myGuiControlImage.Size = button.Size * 0.6f;
            return myGuiControlImage;
        }

        // From Sandbox.Game.Screens.MyGuiScreenNewWorkshopGame
        private void AdjustButtonForIcon(MyGuiControlButton button, MyGuiControlImage icon)
        {
            button.Size = new Vector2(button.Size.X, button.Size.X * 4f / 3f);
            button.HighlightChanged += delegate (MyGuiControlBase x)
            {
                icon.ColorMask = (x.HasHighlight ? MyGuiConstants.HIGHLIGHT_TEXT_COLOR : Vector4.One);
            };
        }

        private void OnRateUpClicked(MyGuiControlButton button)
        {
            UpdateRateState(positive: RateStatus.RatedDown);
        }

        private void OnRateDownClicked(MyGuiControlButton button)
        {
            UpdateRateState(positive: RateStatus.RatedDown);
        }

        private void UpdateRateState(RateStatus positive)
        {
            if (selectedPlugin == null)
                return;

            selectedPlugin.Rate(positive);

            switch (positive)
            {
                case RateStatus.RatedUp:
                    buttonRateUp.Checked = true;
                    break;

                case RateStatus.RatedDown:
                    buttonRateDown.Checked = true;
                    break;
            }

            RefreshPluginStats();
        }
        #endregion

        #region Restart
        private void AskRestartResult(ResultEnum result)
        {
            if (result == ResultEnum.YES)
            {
                Save();
                if (MyGuiScreenGamePlay.Static != null)
                {
                    ShowSaveMenu(delegate
                    {
                        UnloadAndRestartGame();
                    });
                    return;
                }
                UnloadAndRestartGame();
            }
            else if (result == ResultEnum.NO)
            {
                Save();
                CloseScreen();
            }
        }

        /// <summary>
        /// From WesternGamer/InGameWorldLoading
        /// </summary>
        /// <param name="afterMenu">Action after code is executed.</param>
        private static void ShowSaveMenu(Action afterMenu)
        {
            //Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                afterMenu();
                return;
            }

            string message = "";
            bool isCampaign = false;
            MyMessageBoxButtonsType buttonsType = MyMessageBoxButtonsType.YES_NO_CANCEL;

            //Sync.IsServer is backwards
            if (Sync.IsServer && !MySession.Static.Settings.EnableSaving)
            {
                message += "Are you sure that you want to restart the game? All progress from the last checkpoint will be lost.";
                isCampaign = true;
                buttonsType = MyMessageBoxButtonsType.YES_NO;
            }
            else
            {
                message += "Save changes before restarting game?";
            }

            MyGuiScreenMessageBox saveMenu = MyGuiSandbox.CreateMessageBox(buttonType: buttonsType, messageText: new StringBuilder(message), messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm), callback: ShowSaveMenuCallback, cancelButtonText: MyStringId.GetOrCompute("Don't Restart"));
            saveMenu.InstantClose = false;
            MyGuiSandbox.AddScreen(saveMenu);

            void ShowSaveMenuCallback(ResultEnum callbackReturn)
            {
                if (isCampaign)
                {
                    if (callbackReturn == ResultEnum.YES)
                        afterMenu();

                    return;
                }

                switch (callbackReturn)
                {
                    case ResultEnum.YES:
                        MyAsyncSaving.Start(delegate {
                            MySandboxGame.Static.OnScreenshotTaken += UnloadAndExitAfterScreenshotWasTaken;
                        });
                        break;

                    case ResultEnum.NO:
                        MyAudio.Static.Mute = true;
                        MyAudio.Static.StopMusic();
                        afterMenu();
                        break;
                }
            }

            void UnloadAndExitAfterScreenshotWasTaken(object sender, EventArgs e)
            {
                MySandboxGame.Static.OnScreenshotTaken -= UnloadAndExitAfterScreenshotWasTaken;
                afterMenu();
            }
        }

        private static void UnloadAndRestartGame()
        {
            MySessionLoader.Unload();
            MySandboxGame.Config.ControllerDefaultOnStart = MyInput.Static.IsJoystickLastUsed;
            MySandboxGame.Config.Save();
            MyScreenManager.CloseAllScreensNowExcept(null);
            LoaderTools.Restart();
        }
        #endregion
    }
}