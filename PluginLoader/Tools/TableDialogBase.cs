using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Sandbox;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.Tools
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public abstract class TableDialogBase : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName() => "ListDialog";

        protected MyGuiControlButton LoadButton;
        protected MyGuiControlButton RenameButton;
        protected MyGuiControlButton DeleteButton;
        protected MyGuiControlButton CancelButton;

        protected readonly string Caption;
        protected readonly string DefaultKey;

        protected MyGuiControlTable Table;
        protected readonly Dictionary<string, string> NamesByKey = new();

        protected abstract string ItemName { get; }
        protected abstract string[] ColumnHeaders { get; }
        protected abstract float[] ColumnWidths { get; }
        protected virtual string NormalizeName(string name) => name.Trim();
        protected virtual int CompareNames(string a, string b) => string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);

        protected abstract IEnumerable<string> IterItemKeys();
        protected abstract ItemView GetItemView(string key);
        protected abstract object[] ExampleValues {get;}

        protected abstract void OnLoad(string key);
        protected abstract void OnRenamed(string key, string name);
        protected abstract void OnDelete(string key);

        protected int ColumnCount;

        protected TableDialogBase(
            string caption,
            string defaultKey = null)
            : base(new Vector2(0.5f, 0.5f), new Vector2(1f, 0.8f), MyGuiConstants.SCREEN_BACKGROUND_COLOR * MySandboxGame.Config.UIBkOpacity, true)
        {
            Caption = caption;
            DefaultKey = defaultKey;

            // ReSharper disable once VirtualMemberCallInConstructor
            RecreateControls(true);

            CanBeHidden = true;
            CanHideOthers = true;
            CloseButtonEnabled = true;

            OnEnterCallback = LoadAndClose;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(Caption, Color.White.ToVector4(), new Vector2(0.0f, 0.003f));

            CreateTable();
            CreateButtons();
        }

        private Vector2 DialogSize => m_size ?? Vector2.One;

        private void CreateTable()
        {
            var columnHeaders = ColumnHeaders;
            var columnWidths = ColumnWidths;

            if (columnHeaders == null || columnWidths == null)
            {
                Debug.Assert(columnHeaders != null);
                Debug.Assert(columnWidths != null);
                return;
            }

            ColumnCount = columnHeaders.Length;
            Debug.Assert(ColumnCount > 0);
            Debug.Assert(ColumnCount <= 50);
            Debug.Assert(ColumnCount == columnWidths.Length);

            Table = new MyGuiControlTable
            {
                Position = new Vector2(0.001f, -0.5f * DialogSize.Y + 0.1f),
                Size = new Vector2(0.85f * DialogSize.X, DialogSize.Y - 0.25f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                ColumnsCount = ColumnCount,
                VisibleRowsCount = 15,
            };

            Table.SetCustomColumnWidths(columnWidths);

            var exampleValues = ExampleValues;
            Debug.Assert(exampleValues.Length == ColumnCount);
            for (var colIdx = 0; colIdx < ColumnCount; colIdx++)
            {
                Table.SetColumnName(colIdx, new StringBuilder(columnHeaders[colIdx]));

                switch (exampleValues[colIdx])
                {
                    case int _:
                        Table.SetColumnComparison(colIdx, CellIntComparison);
                        break;

                    default:
                        Table.SetColumnComparison(colIdx, CellTextComparison);
                        break;
                }
            }

            AddItems();

            Table.SortByColumn(0);

            Table.ItemDoubleClicked += OnItemDoubleClicked;

            Controls.Add(Table);
        }

        private int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            var a = NormalizeName(x.Text.ToString());
            var b = NormalizeName(y.Text.ToString());
            return CompareNames(a, b);
        }

        private int CellIntComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            return (x.UserData as int? ?? 0) - (y.UserData as int? ?? 0);
        }

        private void CreateButtons()
        {
            LoadButton = new MyGuiControlButton(
                visualStyle: MyGuiControlButtonStyleEnum.Default,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                text: new StringBuilder("Load"), onButtonClick: OnLoadButtonClick);

            RenameButton = new MyGuiControlButton(
                visualStyle: MyGuiControlButtonStyleEnum.Small,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                text: new StringBuilder("Rename"), onButtonClick: OnRenameButtonClick);

            DeleteButton = new MyGuiControlButton(
                visualStyle: MyGuiControlButtonStyleEnum.Small,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                text: new StringBuilder("Delete"), onButtonClick: OnDeleteButtonClick);

            CancelButton = new MyGuiControlButton(
                visualStyle: MyGuiControlButtonStyleEnum.Small,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelButtonClick);

            var xs = 0.85f * DialogSize.X;
            var y = 0.5f * (DialogSize.Y - 0.15f);
            LoadButton.Position = new Vector2(-0.39f * xs, y);
            RenameButton.Position = new Vector2(-0.12f * xs, y);
            DeleteButton.Position = new Vector2(0.06f * xs, y);
            CancelButton.Position = new Vector2(0.42f * xs, y);

            LoadButton.SetToolTip($"Loads the selected {ItemName}");
            RenameButton.SetToolTip($"Renames the selected {ItemName}");
            DeleteButton.SetToolTip($"Deletes the selected {ItemName}");
            CancelButton.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipOptionsSpace_Cancel));

            Controls.Add(LoadButton);
            Controls.Add(RenameButton);
            Controls.Add(DeleteButton);
            Controls.Add(CancelButton);
        }

        private void AddItems()
        {
            NamesByKey.Clear();

            foreach (var key in IterItemKeys())
                AddRow(key);

            if (TryFindRow(DefaultKey, out var rowIdx))
                Table.SelectedRowIndex = rowIdx;
        }

        private void AddRow(string key)
        {
            var view = GetItemView(key);
            if (view == null)
                return;

            Debug.Assert(view.Labels.Length == ColumnCount);
            Debug.Assert(view.Values.Length == ColumnCount);

            var row = new MyGuiControlTable.Row(key);
            for (var i = 0; i < ColumnCount; i++)
                row.AddCell(new MyGuiControlTable.Cell(view.Labels[i], view.Values[i]));

            Table.Add(row);
            NamesByKey[key] = view.Labels[0];
        }

        private void OnItemDoubleClicked(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            LoadAndClose();
        }

        private void OnLoadButtonClick(MyGuiControlButton _) => LoadAndClose();

        private void LoadAndClose()
        {
            if (string.IsNullOrEmpty(SelectedKey))
                return;

            OnLoad(SelectedKey);
            CloseScreen();
        }

        private void OnCancelButtonClick(MyGuiControlButton _) => CloseScreen();

        private void OnRenameButtonClick(MyGuiControlButton _)
        {
            if (string.IsNullOrEmpty(SelectedKey))
                return;

            if (!NamesByKey.TryGetValue(SelectedKey, out var oldName))
                return;

            MyGuiSandbox.AddScreen(new NameDialog(newName => OnNewNameSpecified(SelectedKey, newName), $"Rename saved {ItemName}", oldName));
        }

        private void OnNewNameSpecified(string key, string newName)
        {
            newName = NormalizeName(newName);

            if (!TryFindRow(key, out var rowIdx))
                return;

            OnRenamed(key, newName);

            var view = GetItemView(key);
            Debug.Assert(view.Labels.Length == ColumnCount);
            Debug.Assert(view.Values.Length == ColumnCount);

            NamesByKey[key] = view.Labels[0];

            var row = Table.GetRow(rowIdx);
            for (var colIdx = 0; colIdx < ColumnCount; colIdx++)
            {
                var cell = row.GetCell(colIdx);
                var sb = cell.Text;
                sb.Clear();
                sb.Append(view.Labels[colIdx]);
            }

            Table.Sort();
        }

        private void OnDeleteButtonClick(MyGuiControlButton _)
        {
            var key = SelectedKey;
            if (key == "")
                return;

            var name = NamesByKey.GetValueOrDefault(key) ?? "?";

            MyGuiSandbox.AddScreen(
                MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.YES_NO,
                    messageText: new StringBuilder($"Are you sure to delete this saved {ItemName}?\r\n\r\n{name}"),
                    messageCaption: new StringBuilder("Confirmation"),
                    callback: result => OnDeleteForSure(result, key)));
        }

        private void OnDeleteForSure(MyGuiScreenMessageBox.ResultEnum result, string key)
        {
            if (result != MyGuiScreenMessageBox.ResultEnum.YES)
                return;

            NamesByKey.Remove(key);

            if (TryFindRow(key, out var rowIdx))
                Table.Remove(Table.GetRow(rowIdx));

            OnDelete(key);
        }

        private string SelectedKey => Table.SelectedRow?.UserData as string;

        private bool TryFindRow(string key, out int index)
        {
            if (key == null)
            {
                index = -1;
                return false;
            }

            var count = Table.RowsCount;
            for (index = 0; index < count; index++)
            {
                if (Table.GetRow(index).UserData as string == key)
                    return true;
            }

            index = -1;
            return false;
        }
    }
}