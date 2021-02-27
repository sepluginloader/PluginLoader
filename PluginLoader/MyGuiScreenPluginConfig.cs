using avaness.PluginLoader.Data;
using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
		private const float barWidth = 0.75f;
		private const float space = 0.01f;
		private const float btnSpace = 0.02f;
		private const float tableWidth = 0.8f;
		private const float tableHeight = 0.7f;
        private const float sizeX = 1;
        private const float sizeY = 0.75f;

		private Dictionary<string, bool> dataChanges = new Dictionary<string, bool>();

		public MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(sizeX, sizeY), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
		{
			EnabledBackgroundFade = true;
			m_closeOnEsc = true;
			m_drawEvenWithoutFocus = true;
			CanHideOthers = true;
			CanBeHidden = true;
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
			var title = AddCaption("Plugin List");

			Vector2 size = m_size.Value;
			Vector2 origin = title.Position;
			
			origin.Y += title.GetTextSize().Y / 2 + space;

			float barWidth = size.X * MyGuiScreenPluginConfig.barWidth;
			MyGuiControlSeparatorList titleBar = new MyGuiControlSeparatorList();
			titleBar.AddHorizontal(new Vector2(origin.X - barWidth / 2, origin.Y), barWidth);
			Controls.Add(titleBar);

			origin.Y += space;

			MyGuiControlTable modTable = new MyGuiControlTable
			{
				Position = origin,
				Size = new Vector2(size.X * tableWidth, size.Y * tableHeight),
				OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
				ColumnsCount = 4,
				VisibleRowsCount = 15
			};
			modTable.SetCustomColumnWidths(new float[]
			{
				0.15f,
				0.4f,
				0.2f,
				0.25f
			});
			modTable.SetColumnName(0, new StringBuilder("Source"));
			modTable.SetColumnName(1, new StringBuilder("Name"));
			modTable.SetColumnName(2, new StringBuilder("Enabled"));
			modTable.SetColumnName(3, new StringBuilder("Status"));
			Controls.Add(modTable);

			origin.Y += modTable.Size.Y + space;

			PluginConfig config = Main.Instance.Config;
			foreach(PluginData data in config.Data.Values)
            {
				MyGuiControlTable.Row row = new MyGuiControlTable.Row(data);
				modTable.Add(row);

				MyGuiControlTable.Cell sourceCell = new MyGuiControlTable.Cell(data.Source);
				if(data is SteamPlugin steam)
                {
					MyGuiControlButton steamLink = new MyGuiControlButton(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, textAlignment: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER, text: new StringBuilder(data.Source), onButtonClick: OnOpenSteamWorkshop);
					steamLink.UserData = steam.WorkshopId;
					steamLink.VisualStyle = MyGuiControlButtonStyleEnum.ClickableText;
					sourceCell.Control = steamLink;
					modTable.Controls.Add(steamLink);
                }
				row.AddCell(sourceCell);

				MyGuiControlTable.Cell nameCell = new MyGuiControlTable.Cell(data.FriendlyName);
				row.AddCell(nameCell);

				MyGuiControlTable.Cell enabledCell = new MyGuiControlTable.Cell();
                MyGuiControlCheckbox enabledBox = new MyGuiControlCheckbox(isChecked: data.Enabled)
                {
                    UserData = data,
                    Enabled = true,
                    Visible = true
                };
                enabledBox.IsCheckedChanged += IsCheckedChanged;
				enabledCell.Control = enabledBox;
                modTable.Controls.Add(enabledBox);
                row.AddCell(enabledCell);

				MyGuiControlTable.Cell updateCell = new MyGuiControlTable.Cell(data.StatusString);
				row.AddCell(updateCell);
            }

            modTable.SelectedRowIndex = null;

			MyGuiControlSeparatorList midBar = new MyGuiControlSeparatorList();
			midBar.AddHorizontal(new Vector2(origin.X - barWidth / 2, origin.Y), barWidth);
			Controls.Add(midBar);

			origin.Y += space;

			MyGuiControlButton btnRestart = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Restart the game and apply changes.", new StringBuilder("Restart"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnOkButtonClick);

			MyGuiControlButton btnSave = new MyGuiControlButton(origin, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, toolTip: "Save changes. Changes will take effect next time the game starts.", text: new StringBuilder("Save"), onButtonClick: OnSaveButtonClick);

			MyGuiControlButton btnClose = new MyGuiControlButton(origin, 0, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, null, new StringBuilder("Close"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnCloseButtonClick);

			AlignRow(origin, btnSpace, btnRestart, btnSave, btnClose);
			Controls.Add(btnRestart);
			Controls.Add(btnSave);
			Controls.Add(btnClose);

			CloseButtonEnabled = true;
        }

        private void OnOpenSteamWorkshop(MyGuiControlButton btn)
        {
			if (btn.UserData is ulong steamId)
				MyGuiSandbox.OpenUrl("https://steamcommunity.com/workshop/filedetails/?id=" + steamId, UrlOpenMode.SteamOrExternalWithConfirm);
        }

        private void OnSaveButtonClick(MyGuiControlButton btn)
        {
			Save();
			CloseScreen();
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
			if (original.Enabled == checkbox.IsChecked)
				dataChanges.Remove(original.Id);
			else
				dataChanges[original.Id] = checkbox.IsChecked;
		}

        private void OnCloseButtonClick(MyGuiControlButton btn)
        {
			dataChanges.Clear();
			CloseScreen();
        }

        private void OnOkButtonClick(MyGuiControlButton btn)
		{
			Save();
			Restart();
		}

		private void Restart()
		{
			Application.Restart();
			Process.GetCurrentProcess().Kill();
		}

		private void Save()
        {
			if(dataChanges.Count > 0)
			{
				PluginConfig config = Main.Instance.Config;
				foreach (KeyValuePair<string, bool> kv in dataChanges)
				{
					if (config.Data.TryGetValue(kv.Key, out PluginData data))
						data.Enabled = kv.Value;
				}
				config.Save();
				dataChanges.Clear();
			}
        }
    }
}
