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
using static Sandbox.Graphics.GUI.MyGuiScreenMessageBox;

namespace avaness.PluginLoader.GUI
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
        private const float BarWidth = 0.85f;
        private const float Spacing = 0.0175f;

        private readonly Dictionary<string, MyGuiControlCheckbox> pluginCheckboxes = new Dictionary<string, MyGuiControlCheckbox>();
        private readonly PluginDetailsPanel pluginDetails = new PluginDetailsPanel();

        private MyGuiControlTable pluginTable;
        private MyGuiControlLabel pluginCountLabel;

        private static PluginConfig Config => Main.Instance.Config;
        private string[] tableFilter;

        private PluginData SelectedPlugin
        {
            get => pluginDetails.Plugin;
            set => pluginDetails.Plugin = value;
        }

        private static bool allItemsVisible = true;

        #region Icons

        // Source: MyTerminalControlPanel
        private static readonly MyGuiHighlightTexture IconHide = new MyGuiHighlightTexture
        {
            Normal = "Textures\\GUI\\Controls\\button_hide.dds",
            Highlight = "Textures\\GUI\\Controls\\button_hide.dds",
            Focus = "Textures\\GUI\\Controls\\button_hide_focus.dds",
            SizePx = new Vector2(40f, 40f)
        };

        // Source: MyTerminalControlPanel
        private static readonly MyGuiHighlightTexture IconShow = new MyGuiHighlightTexture
        {
            Normal = "Textures\\GUI\\Controls\\button_unhide.dds",
            Highlight = "Textures\\GUI\\Controls\\button_unhide.dds",
            Focus = "Textures\\GUI\\Controls\\button_unhide_focus.dds",
            SizePx = new Vector2(40f, 40f)
        };

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

        public override void UnloadContent()
        {
            pluginDetails.OnPluginToggled -= EnablePlugin;
            base.UnloadContent();
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

            #endregion

            origin.Y += searchBox.Size.Y + Spacing;

            #region Plugin List

            // Adds the plugin list on the right of the menu below the search bar.
            pluginTable = new MyGuiControlTable
            {
                Position = new Vector2(origin.X - (BarWidth / 2), origin.Y),
                Size = new Vector2(searchBox.Size.X + buttonVisibility.Size.X + 0.001f, 0.6f), // The y value can be bigger than the visible rows count as the visibleRowsCount controls the height.
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

            // Double clicking list items toggles the enable flag
            pluginTable.ItemDoubleClicked += OnItemDoubleClicked;

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

            // Right side panel showing the details of the selected plugin
            var rightSideOrigin = buttonVisibility.Position + new Vector2(Spacing * 1.778f + (buttonVisibility.Size.X / 2), -(buttonVisibility.Size.Y / 2));
            pluginDetails.CreateControls(rightSideOrigin);
            Controls.Add(pluginDetails);
            pluginDetails.OnPluginToggled += EnablePlugin;

            // Refreshes the table to show plugins on plugin list
            RefreshTable();
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

        private static int CellTextOrDataComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            int result = TextComparison(x.Text, y.Text);
            if (result != 0)
            {
                return result;
            }

            return TextComparison((StringBuilder)x.UserData, (StringBuilder)y.UserData);
        }

        private static int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            return TextComparison(x.Text, y.Text);
        }

        private static int TextComparison(StringBuilder x, StringBuilder y)
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
        /// Clears the table and adds the list of plugins and their information.
        /// </summary>
        /// <param name="filter">Text filter</param>
        private void RefreshTable(string[] filter = null)
        {
            pluginTable.Clear();
            pluginTable.Controls.Clear();
            pluginCheckboxes.Clear();
            var list = Main.Instance.List;
            var noFilter = filter == null || filter.Length == 0;
            foreach (var plugin in list)
            {
                var enabled = plugin.EnableAfterRestart;

                if (noFilter && (plugin.Hidden || !allItemsVisible) && !enabled)
                    continue;

                if (!noFilter && !FilterName(plugin.FriendlyName, filter))
                    continue;

                var row = new MyGuiControlTable.Row(plugin);
                pluginTable.Add(row);

                var name = new StringBuilder(plugin.FriendlyName);
                row.AddCell(new MyGuiControlTable.Cell(plugin.Source, name));

                var tip = plugin.FriendlyName;
                if (!string.IsNullOrWhiteSpace(plugin.Tooltip))
                    tip += "\n" + plugin.Tooltip;
                row.AddCell(new MyGuiControlTable.Cell(plugin.FriendlyName, toolTip: tip));

                var text = new StringBuilder(enabled ? "1" : "0");
                var enabledCell = new MyGuiControlTable.Cell(text, name);
                var enabledCheckbox = new MyGuiControlCheckbox(isChecked: enabled)
                {
                    UserData = plugin,
                    Visible = true
                };
                enabledCheckbox.IsCheckedChanged += OnPluginCheckboxChanged;
                enabledCell.Control = enabledCheckbox;
                pluginTable.Controls.Add(enabledCheckbox);
                pluginCheckboxes.Add(plugin.Id, enabledCheckbox);
                row.AddCell(enabledCell);
            }

            pluginCountLabel.Text = pluginTable.RowsCount + " out of the total " + list.Count + " \nplugins are visible.";
            pluginTable.Sort(false);
            pluginTable.SelectedRowIndex = null;
            tableFilter = filter;
            pluginTable.SelectedRowIndex = 0;

            var args = new MyGuiControlTable.EventArgs { RowIndex = 0 };
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
            if (!TryGetPluginByRowIndex(args.RowIndex, out var plugin))
                return;

            SelectedPlugin = plugin;
        }

        private void OnItemDoubleClicked(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            if (!TryGetPluginByRowIndex(args.RowIndex, out var data))
                return;

            Config.SetEnabled(data.Id, !Config.IsEnabled(data.Id));
        }

        private bool TryGetPluginByRowIndex(int rowIndex, out PluginData plugin)
        {
            if (rowIndex < 0 || rowIndex >= pluginTable.RowsCount)
            {
                plugin = null;
                return false;
            }

            var row = pluginTable.GetRow(rowIndex);
            plugin = row.UserData as PluginData;
            return plugin != null;
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

        private void OnPluginCheckboxChanged(MyGuiControlCheckbox checkbox)
        {
            var plugin = (PluginData)checkbox.UserData;
            EnablePlugin(plugin, checkbox.IsChecked);

            if (ReferenceEquals(plugin, SelectedPlugin))
                pluginDetails.LoadPluginData();
        }

        private void EnablePlugin(PluginData plugin, bool enable)
        {
            if (enable == plugin.EnableAfterRestart)
                return;

            plugin.EnableAfterRestart = enable;

            SetPluginCheckbox(plugin, enable);

            if (plugin.EnableAfterRestart)
                DisableOtherPluginsInSameGroup(plugin);
        }

        private void SetPluginCheckbox(PluginData plugin, bool enable)
        {
            var checkbox = pluginCheckboxes[plugin.Id];
            checkbox.IsChecked = enable;

            var row = pluginTable.Find(x => ReferenceEquals(x.UserData as PluginData, plugin));
            row?.GetCell(2).Text.Clear().Append(enable ? "1" : "0");
        }

        private void DisableOtherPluginsInSameGroup(PluginData plugin)
        {
            foreach (var other in plugin.Group)
                if (!ReferenceEquals(other, plugin))
                    EnablePlugin(other, false);
        }

        private void OnCloseButtonClick(MyGuiControlButton btn)
        {
            CloseScreen();
        }

        private void OnRestartButtonClick(MyGuiControlButton btn)
        {
            if (Main.Instance.List.ModifiedCount == 0)
            {
                CloseScreen();
                return;
            }

            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, MyMessageBoxButtonsType.YES_NO_CANCEL, new StringBuilder("A restart is required to apply changes. Would you like to restart the game now?"), new StringBuilder("Apply Changes?"), callback: AskRestartResult));
        }

        private void Save()
        {
            if (Main.Instance.List.ModifiedCount == 0)
                return;

            foreach (var plugin in Main.Instance.List)
                Config.SetEnabled(plugin.Id, plugin.EnableAfterRestart);

            Config.Save();
        }

        #region Restart

        private void AskRestartResult(ResultEnum result)
        {
            if (result == ResultEnum.YES)
            {
                Save();
                if (MyGuiScreenGamePlay.Static != null)
                {
                    ShowSaveMenu(delegate { UnloadAndRestartGame(); });
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
            // Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                afterMenu();
                return;
            }

            string message = "";
            bool isCampaign = false;
            MyMessageBoxButtonsType buttonsType = MyMessageBoxButtonsType.YES_NO_CANCEL;

            // Sync.IsServer is backwards
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
                        MyAsyncSaving.Start(delegate { MySandboxGame.Static.OnScreenshotTaken += UnloadAndExitAfterScreenshotWasTaken; });
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