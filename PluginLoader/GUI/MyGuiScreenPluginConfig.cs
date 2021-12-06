using avaness.PluginLoader.Data;
using avaness.PluginLoader.GUI.GuiControls;
using Sandbox;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
		private const float barWidth = 0.912f;
		private const float space = 0.01f;
		private const float btnSpace = 0.02f;
		private const float tableWidth = 0.4f;
		private const float tableHeight = 0.7f;
        private const float sizeX = 0.878f;
        private const float sizeY = 0.97f;

		private readonly Dictionary<string, bool> dataChanges = new Dictionary<string, bool>();
		private readonly Dictionary<string, MyGuiControlCheckbox> dataCheckboxes = new Dictionary<string, MyGuiControlCheckbox>();
		private MyGuiControlTable modTable;
		private MyGuiControlLabel countLabel;
		private PluginConfig config;
		private string[] tableFilter;

		private static bool allItemsVisible = true;

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

        public MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(sizeX, sizeY), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
		{
			EnabledBackgroundFade = true;
			m_closeOnEsc = true;
			m_drawEvenWithoutFocus = true;
			CanHideOthers = true;
			CanBeHidden = true;
			config = Main.Instance.Config;
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

		public override void RecreateControls(bool constructor)
		{
			base.RecreateControls(constructor);

			config = Main.Instance.Config;

			MyGuiControlLabel title = AddCaption("Plugin List");

			Vector2 size = m_size.Value;
			Vector2 origin = title.Position;

			Vector2 tableOffSet = new Vector2(-0.576f, -0.432f);
			Vector2 searchBoxOffSet = new Vector2(-0.576f, -0.471f);
			Vector2 visibilityButtonOffSet = new Vector2(-0.2525f, -0.471f);			
			Vector2 minSizeGui = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;

			origin.Y += title.GetTextSize().Y / 2 + space;

			float barWidth = size.X * MyGuiScreenPluginConfig.barWidth;
			MyGuiControlSeparatorList titleBar = new MyGuiControlSeparatorList();
			titleBar.AddHorizontal(new Vector2(origin.X - barWidth / 2, origin.Y), barWidth);
			Controls.Add(titleBar);

			origin.Y += space;

			float totalTableWidth = size.X * tableWidth;

			MyGuiControlSearchBox searchBox = new MyGuiControlSearchBox(searchBoxOffSet + new Vector2(minSizeGui.X, 0.08f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
			float extraSpaceWidth = searchBox.Size.Y;
			searchBox.Size = new Vector2(totalTableWidth - extraSpaceWidth, searchBox.Size.Y);
            searchBox.OnTextChanged += SearchBox_TextChanged;
			Controls.Add(searchBox);

			MyGuiControlButton btnVisibility = new MyGuiControlButton(visibilityButtonOffSet + new Vector2(minSizeGui.X, 0.08f), MyGuiControlButtonStyleEnum.SquareSmall, onButtonClick: OnVisibilityClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, toolTip: "Toggle hidden plugins.", buttonScale: 0.5f);

			if (allItemsVisible || config.Count == 0)
            {
				allItemsVisible = true;
                btnVisibility.Icon = IconHide;
            }
            else
            {
                btnVisibility.Icon = IconShow;
            } 

            Controls.Add(btnVisibility);

			origin.Y += searchBox.Size.Y + MyGuiConstants.TEXTBOX_TEXT_OFFSET.Y;

			modTable = new MyGuiControlTable
			{
				Position = tableOffSet + new Vector2(minSizeGui.X, 0.08f),
				Size = new Vector2(totalTableWidth, size.Y * tableHeight),
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
				ColumnsCount = 2,
				VisibleRowsCount = 21
			};
			modTable.SetCustomColumnWidths(new float[]
			{
				0.267f,
				0.77f,
			});     
			modTable.SetColumnName(0, new StringBuilder("Source"));
			modTable.SetColumnComparison(0, CellTextOrDataComparison);
			modTable.SetColumnName(1, new StringBuilder("Name"));
			modTable.SetColumnComparison(1, CellTextComparison);

			modTable.SortByColumn(1);

			modTable.ItemDoubleClicked += RowDoubleClicked;
			modTable.ItemSelected += OnItemSelected;
			Controls.Add(modTable);

			origin.Y += modTable.Size.Y + space;

			countLabel = new MyGuiControlLabel(new Vector2(origin.X - (modTable.Size.X * 0.5f), origin.Y), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
			Controls.Add(countLabel);

			ResetTable();

			MyGuiControlSeparatorList midBar = new MyGuiControlSeparatorList();
            midBar.AddHorizontal(new Vector2(origin.X - barWidth / 2, origin.Y), barWidth);
			Controls.Add(midBar);

			origin.Y += space;

			MyGuiControlButton btnRestart = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Restart the game and apply changes.", new StringBuilder("Apply"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnRestartButtonClick);

			MyGuiControlButton btnClose = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, null, new StringBuilder("Cancel"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnCloseButtonClick);
			
			MyGuiControlButton btnShow = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Open the source of the selected plugin.", new StringBuilder("Info"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnInfoButtonClick);

			AlignRow(origin, btnSpace, btnRestart, btnClose, btnShow);
			Controls.Add(btnRestart);
			Controls.Add(btnClose);
			Controls.Add(btnShow);

			CloseButtonEnabled = true;
		}

        private void RefreshRight(PluginData data)
        {
			// Hides existing controls so the old and new won't overlap. There is no way to remove the existing controls. OnRemoving just sets the controls in their default position.
			if (pluginNameLabel != null)
			{
				pluginNameLabel.Visible = false;
			}
			if (pluginNameText != null)
			{
				pluginNameText.Visible = false;
			}
			if (authorLabel != null)
            {
				authorLabel.Visible = false;
			}
			if (authorText != null)
			{
				authorText.Visible = false;
			}
			if (versionLabel != null)
            {
				versionLabel.Visible = false;
			}
			if (versionText != null)
            {
				versionText.Visible = false;
			}
			if (statusLabel != null)
            {
				statusLabel.Visible = false;
			}
			if (statusText != null)
            {
				statusText.Visible = false;
			}
			if (usageLabel != null)
            {
				usageLabel.Visible = false;
			}
			if (usageText != null)
			{
				usageText.Visible = false;
			}
			if (ratingLabel != null)
			{
				ratingLabel.Visible = false;
			}
			if (ratingDisplay != null)
			{
				ratingDisplay.Visible = false;
			}
			if (buttonRateUp != null)
			{
				buttonRateUp.Visible = false;
			}
			if (iconRateUp != null)
			{
				iconRateUp.Visible = false;
			}
			if (buttonRateDown != null)
			{
				buttonRateDown.Visible = false;
			}
			if (iconRateDown != null)
			{
				iconRateDown.Visible = false;
			}
			if (descriptionText != null)
			{
				descriptionText.Visible = false;
			}
			if (descriptionPanel != null)
			{
				descriptionPanel.Visible = false;
			}
			if (toggleButtonLabel != null)
			{
				toggleButtonLabel.Visible = false;
			}
			if (toggleButton != null)
			{
				toggleButton.Visible = false;
			}


			Vector2 layoutTableOffset = new Vector2(-0.2f, -0.471f);
			Vector2 minSizeGui = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
			layoutTable = new MyLayoutTable(this, layoutTableOffset + new Vector2(minSizeGui.X, 0.067f), new Vector2(1f, 1f));
			layoutTable.SetColumnWidths(345f, 345f);
			layoutTable.SetRowHeights(75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f, 75f);
			

			pluginNameLabel = new MyGuiControlLabel
			{
				Text = "Plugin Name:",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			pluginNameText = new MyGuiControlLabel
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			authorLabel = new MyGuiControlLabel
			{
				Text = "Author:",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			authorText = new MyGuiControlLabel
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			versionLabel = new MyGuiControlLabel
			{
				Text = "Version:",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			versionText = new MyGuiControlLabel
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			statusLabel = new MyGuiControlLabel
			{
				Text = "Status:",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			statusText = new MyGuiControlLabel
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			usageLabel = new MyGuiControlLabel
			{
				Text = "Usage:",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			usageText = new MyGuiControlLabel
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			ratingLabel = new MyGuiControlLabel
			{
				Text = "Rating:",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			ratingDisplay = new RatingControl(10)
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER
			};

			buttonRateUp = CreateRateButton(positive: true);
			iconRateUp = CreateRateIcon(buttonRateUp, "Textures\\GUI\\Icons\\Blueprints\\like_test.png");

			buttonRateDown = CreateRateButton(positive: false);
			iconRateDown = CreateRateIcon(buttonRateDown, "Textures\\GUI\\Icons\\Blueprints\\dislike_test.png");

			descriptionText = new MyGuiControlMultilineText(null, null, null, "Blue", 0.8f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, drawScrollbarV: true, drawScrollbarH: true, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, selectable: false, showTextShadow: false, null, null)
			{
				Name = "DescriptionText",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
				TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
				TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
			};

			descriptionPanel = new MyGuiControlCompositePanel
			{
				BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK_BORDER
			};

			toggleButtonLabel = new MyGuiControlLabel
			{
				Text = "On/Off",
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
			};

			toggleButton = new MyGuiControlCheckbox(toolTip: "Enables or disables the plugin.")
			{
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
				Enabled = false
			};

			// Left side of table.
			layoutTable.Add(pluginNameLabel, MyAlignH.Left, MyAlignV.Center, 0, 0);
			layoutTable.Add(authorLabel, MyAlignH.Left, MyAlignV.Center, 1, 0);

			int rowLeft;

			if (data.Version != null || data.Status != PluginStatus.None)
            {
				layoutTable.Add(versionLabel, MyAlignH.Left, MyAlignV.Center, 2, 0);
				layoutTable.Add(statusLabel, MyAlignH.Left, MyAlignV.Center, 3, 0);
				rowLeft = 4;
			}
            else
            {
				rowLeft = 2;
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


			// Right side of table.
			layoutTable.Add(pluginNameText, MyAlignH.Left, MyAlignV.Center, 0, 1);
			layoutTable.Add(authorText, MyAlignH.Left, MyAlignV.Center, 1, 1);

			int rowRight;

			if (data.Version != null || data.Status != PluginStatus.None)
			{
				layoutTable.Add(versionText, MyAlignH.Left, MyAlignV.Center, 2, 1);
				layoutTable.Add(statusText, MyAlignH.Left, MyAlignV.Center, 3, 1);
				rowRight = 4;
			}
			else
			{
				rowRight = 2;
			}

			layoutTable.Add(usageText, MyAlignH.Left, MyAlignV.Center, rowRight, 1);
			++rowRight;
			layoutTable.Add(ratingDisplay, MyAlignH.Left, MyAlignV.Center, rowRight, 1);
			layoutTable.Add(buttonRateUp, MyAlignH.Right, MyAlignV.Center, rowRight, 1);
			layoutTable.Add(iconRateUp, MyAlignH.Center, MyAlignV.Center, rowRight, 1);
			layoutTable.Add(buttonRateDown, MyAlignH.Right, MyAlignV.Center, rowRight, 1);
			layoutTable.Add(iconRateDown, MyAlignH.Center, MyAlignV.Center, rowRight, 1);
			rowRight += 4;
			layoutTable.Add(toggleButton, MyAlignH.Left, MyAlignV.Top, rowRight, 1);
			buttonRateUp.PositionX -= 0.05f;
			iconRateUp.Position = buttonRateUp.Position + new Vector2(-0.0015f, -0.002f) - new Vector2(buttonRateUp.Size.X / 2f, 0f);
			iconRateDown.Position = buttonRateDown.Position + new Vector2(-0.0015f, -0.002f) - new Vector2(buttonRateDown.Size.X / 2f, 0f);

		}

        private void OnVisibilityClick(MyGuiControlButton btn)
        {
			if(allItemsVisible)
            {
				allItemsVisible = false;
				btn.Icon = IconShow;
            }
			else
            {
				allItemsVisible = true;
				btn.Icon = IconHide;
            }
			ResetTable(tableFilter);
        }

        private int CellCheckedOrDataComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
			if(x.Control is MyGuiControlCheckbox xBox && y.Control is MyGuiControlCheckbox yBox)
            {
				int result = yBox.IsChecked.CompareTo(xBox.IsChecked);
				if (result != 0)
					return result;
			} 
			return TextComparison((StringBuilder)x.UserData, (StringBuilder)y.UserData);
        }

		private int CellTextOrDataComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
			int result = TextComparison(x.Text, y.Text);
			if (result != 0)
				return result;
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

        private void ResetTable(string[] filter = null)
		{
			modTable.Clear();
			modTable.Controls.Clear();
			dataCheckboxes.Clear();
			PluginList list = Main.Instance.List;
			bool noFilter = filter == null || filter.Length == 0;
			foreach (PluginData data in list)
			{
				bool enabled;
				if(!dataChanges.TryGetValue(data.Id, out enabled))
					enabled = config.IsEnabled(data.Id);

                if (noFilter && (data.Hidden || !allItemsVisible) && !enabled)
                    continue;

                if (noFilter || FilterName(data.FriendlyName, filter))
				{
					MyGuiControlTable.Row row = new MyGuiControlTable.Row(data);
					modTable.Add(row);
					StringBuilder name = new StringBuilder(data.FriendlyName);

                    row.AddCell(new MyGuiControlTable.Cell(data.Source, name));

					string tip = data.FriendlyName;
					if (!string.IsNullOrWhiteSpace(data.Tooltip))
						tip += "\n" +  data.Tooltip;
                    row.AddCell(new MyGuiControlTable.Cell(data.FriendlyName, toolTip: tip));

					MyGuiControlTable.Cell enabledCell = new MyGuiControlTable.Cell(userData: name);
					MyGuiControlCheckbox enabledBox = new MyGuiControlCheckbox(isChecked: enabled)
					{
						UserData = data,
						Visible = true
					};
					
					enabledCell.Control = enabledBox;
					modTable.Controls.Add(enabledBox);
					dataCheckboxes.Add(data.Id, enabledBox);
					row.AddCell(enabledCell);
				}
			}
			countLabel.Text = modTable.RowsCount + "/" + list.Count;
			modTable.Sort(false);
			modTable.SelectedRowIndex = null;
			tableFilter = filter;
		}

		private void SearchBox_TextChanged(string txt)
        {
			string[] args = txt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			ResetTable(args);
        }

		private bool FilterName(string name, string[] filter)
        {
			foreach(string s in filter)
            {
				if (!name.Contains(s, StringComparison.OrdinalIgnoreCase))
					return false;
            }
			return true;
        }

		private void RowDoubleClicked(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
			int i = args.RowIndex;
			if (i >= 0 && i < table.RowsCount)
            {
                MyGuiControlTable.Row row = table.GetRow(i);
				if (row.UserData is PluginData data)
					data.Show();
			}
        }

		//Sets data on right when you select a plugin.
		private void OnItemSelected(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
		{
			int i = args.RowIndex;
			if (i >= 0 && i < table.RowsCount)
			{
				MyGuiControlTable.Row row = table.GetRow(i);
				if (row.UserData is PluginData data)
                {
					RefreshRight(data);
					pluginNameText.Text = data.FriendlyName;

					if (data.Author != null)
                    {          
						authorText.Text = data.Author;
					}
                    else
                    {
						authorText.Text = "Name is not available";
                    }

					if (data.Version != null)
					{
						versionText.Text = data.Version.ToString();
					}
					else
					{
						//TODO: hide the whole thing.
						versionText.Text = "";
					}

					if (data.Status != PluginStatus.None)
					{
						statusText.Text = data.StatusString;
					}
					else
					{
						//TODO: hide the whole thing.
						statusText.Text = "";
					}

					if (data.Tooltip != null)
                    {
						descriptionText.Text = new StringBuilder(data.Tooltip);
					}
                    else
                    {
						descriptionText.Text = new StringBuilder("Description is not available");
					}

					toggleButton.UserData = data;
					toggleButton.IsCheckedChanged += IsCheckedChanged;

					bool enabled;
					if (!dataChanges.TryGetValue(data.Id, out enabled))
						enabled = config.IsEnabled(data.Id);
					
					toggleButton.Enabled = true;

					if (enabled)
                    {
						toggleButton.IsChecked = true;
                    }
                    else
                    {
						toggleButton.IsChecked = false;
					}

					
					
				}	
			}
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
			foreach(var btn in elements)
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
			if(original.Group.Count > 0 && checkbox.IsChecked)
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
            if (config.IsEnabled(original.Id) == enabled)
            {
				return dataChanges.Remove(original.Id);
			}
			else
            {
				dataChanges[original.Id] = enabled;
				return true;
			}
		}

		private void OnInfoButtonClick(MyGuiControlButton btn)
		{
			if(modTable.SelectedRowIndex.HasValue)
            {
				PluginData data = modTable.SelectedRow.UserData as PluginData;
				if (data != null)
					data.Show();
            }
		}

		private void OnCloseButtonClick(MyGuiControlButton btn)
        {
			dataChanges.Clear();
			CloseScreen();
        }

        private void OnRestartButtonClick(MyGuiControlButton btn)
		{
			if(dataChanges.Count == 0)
            {
				CloseScreen();
            }
			else
			{
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, MyMessageBoxButtonsType.YES_NO_CANCEL, new StringBuilder("A restart is required to apply changes. Would you like to restart the game now?"), new StringBuilder("Apply Changes"), callback: AskRestartResult));
			}
		}

        private void AskRestartResult(MyGuiScreenMessageBox.ResultEnum result)
        {
			if(result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
				Save();
				LoaderTools.Restart();
			}
			else if(result == MyGuiScreenMessageBox.ResultEnum.NO)
            {
				Save();
				CloseScreen();
            }
        }

        private void Save()
        {
			if(dataChanges.Count > 0)
			{
				PluginConfig config = Main.Instance.Config;
				foreach (KeyValuePair<string, bool> kv in dataChanges)
					config.SetEnabled(kv.Key, kv.Value);
				config.Save();
				dataChanges.Clear();
			}
        }

		// From Sandbox.Game.Screens.MyGuiScreenNewWorkshopGame
		private MyGuiControlButton CreateRateButton(bool positive)
		{
			return new MyGuiControlButton(null, MyGuiControlButtonStyleEnum.Rectangular, onButtonClick: positive ? new Action<MyGuiControlButton>(OnRateUpClicked) : new Action<MyGuiControlButton>(OnRateDownClicked), size: new Vector2(0.03f));
		}

		// From Sandbox.Game.Screens.MyGuiScreenNewWorkshopGame
		private MyGuiControlImage CreateRateIcon(MyGuiControlButton button, string texture)
		{
			MyGuiControlImage myGuiControlImage = new MyGuiControlImage(null, null, null, null, new string[1] { texture });
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
			UpdateRateState(positive: true);
		}

		private void OnRateDownClicked(MyGuiControlButton button)
		{
			UpdateRateState(positive: false);
		}

		private void UpdateRateState(bool positive)
		{
			//if (m_selectedWorkshopItem != null)
			{
				//m_selectedWorkshopItem.Rate(positive);
				//m_selectedWorkshopItem.ChangeRatingValue(positive);
				buttonRateUp.Checked = positive;
				buttonRateDown.Checked = !positive;
			}
		}
	}
}
