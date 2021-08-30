using avaness.PluginLoader.Data;
using Sandbox;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Audio;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
		private const float barWidth = 0.75f;
		private const float space = 0.01f;
		private const float btnSpace = 0.02f;
		private const float tableWidth = 0.8f;
		private const float tableHeight = 0.7f;
        private const float sizeX = 1;
        private const float sizeY = 0.76f;

		private readonly Dictionary<string, bool> dataChanges = new Dictionary<string, bool>();
		private readonly Dictionary<string, MyGuiControlCheckbox> dataCheckboxes = new Dictionary<string, MyGuiControlCheckbox>();
		private MyGuiControlTable modTable;
		private MyGuiControlLabel countLabel;
		private PluginConfig config;
		private string[] tableFilter;

		private static bool allItemsVisible = false;

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

			var title = AddCaption("Plugin List");

			Vector2 size = m_size.Value;
			Vector2 origin = title.Position;

			origin.Y += title.GetTextSize().Y / 2 + space;

			float barWidth = size.X * MyGuiScreenPluginConfig.barWidth;
			MyGuiControlSeparatorList titleBar = new MyGuiControlSeparatorList();
			titleBar.AddHorizontal(new Vector2(origin.X - barWidth / 2, origin.Y), barWidth);
			Controls.Add(titleBar);

			origin.Y += space;

			float totalTableWidth = size.X * tableWidth;

			MyGuiControlSearchBox searchBox = new MyGuiControlSearchBox(origin, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
			float extraSpaceWidth = searchBox.Size.Y;
			searchBox.Size = new Vector2(totalTableWidth - extraSpaceWidth, searchBox.Size.Y);
			searchBox.Position = new Vector2(origin.X - (extraSpaceWidth / 2), origin.Y);
            searchBox.OnTextChanged += SearchBox_TextChanged;
			Controls.Add(searchBox);

			MyGuiControlButton btnVisibility = new MyGuiControlButton(new Vector2(origin.X + (searchBox.Size.X / 2), origin.Y), MyGuiControlButtonStyleEnum.SquareSmall, new Vector2(extraSpaceWidth), onButtonClick: OnVisibilityClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, toolTip: "Toggle visibility", buttonScale: 0.5f);

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
				Position = origin,
				Size = new Vector2(totalTableWidth, size.Y * tableHeight),
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
				ColumnsCount = 6,
				VisibleRowsCount = 15
			};
			modTable.SetCustomColumnWidths(new float[]
			{
				0.13f,
				0.34f,
				0.18f,
				0.1f,
				0.15f,
				0.1f,
			});
			modTable.SetColumnName(0, new StringBuilder("Source"));
			modTable.SetColumnComparison(0, CellTextComparison);
			modTable.SetColumnName(1, new StringBuilder("Name"));
			modTable.SetColumnComparison(1, CellTextComparison);
			modTable.SetColumnName(2, new StringBuilder("Author"));
			modTable.SetColumnComparison(2, CellTextComparison);
			modTable.SetColumnName(3, new StringBuilder("Version"));
			modTable.SetColumnComparison(3, CellTextComparison);
			modTable.SetColumnName(4, new StringBuilder("Status"));
			modTable.SetColumnComparison(4, CellTextComparison);
			modTable.SetColumnName(5, new StringBuilder("Enabled"));
			modTable.SetColumnComparison(5, CellCheckedComparison);
			modTable.SortByColumn(0);
			modTable.ItemDoubleClicked += RowDoubleClicked;
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

        private int CellCheckedComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
			if(x.Control is MyGuiControlCheckbox xBox && y.Control is MyGuiControlCheckbox yBox)
				return xBox.IsChecked.CompareTo(yBox.IsChecked);
			return 0;
        }

        private int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
			if(x.Text == null)
            {
				if (y.Text == null)
					return 0;
				return 1;
            }

			if (y.Text == null)
				return -1;
			return x.Text.CompareTo(y.Text);
        }

        private void ResetTable(string[] filter = null)
		{
			modTable.Clear();
			modTable.Controls.Clear();
			dataCheckboxes.Clear();
			PluginList list = Main.Instance.List;
			bool noFilter = filter == null || filter.Length == 0;
			foreach (PluginData data in list.OrderBy(x => x.FriendlyName))
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

                    row.AddCell(new MyGuiControlTable.Cell(data.Source));

					string tip = data.FriendlyName;
					if (!string.IsNullOrWhiteSpace(data.Tooltip))
						tip += "\n" +  data.Tooltip;
                    row.AddCell(new MyGuiControlTable.Cell(data.FriendlyName, toolTip: tip));

					row.AddCell(new MyGuiControlTable.Cell(data.Author, toolTip: data.Author));

					row.AddCell(new MyGuiControlTable.Cell(data.Version?.ToString()));

                    row.AddCell(new MyGuiControlTable.Cell(data.StatusString));

					MyGuiControlTable.Cell enabledCell = new MyGuiControlTable.Cell();
					MyGuiControlCheckbox enabledBox = new MyGuiControlCheckbox(isChecked: enabled)
					{
						UserData = data,
						Visible = true
					};
					enabledBox.IsCheckedChanged += IsCheckedChanged;
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
    }
}
