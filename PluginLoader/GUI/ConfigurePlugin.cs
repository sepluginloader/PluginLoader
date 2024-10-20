using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI;

public class ConfigurePlugin : PluginScreen
{
    private readonly List<PluginInstance> pluginInstances;

    private MyGuiControlTable table;
    private MyGuiControlButton btnConfigure, btnCancel;
    private PluginInstance selectedPluginInstance;

    public override string GetFriendlyName()
    {
        return typeof(PluginDetailMenu).FullName;
    }

    public ConfigurePlugin(): base(size: new Vector2(0.7f, 0.9f))
    {
        pluginInstances = Main.Instance.Plugins.Where(p => p.HasConfigDialog).ToList();
    }

    public override void RecreateControls(bool constructor)
    {
        base.RecreateControls(constructor);

        // Top
        var caption = AddCaption("Configure a plugin", captionScale: 1);
        AddBarBelow(caption);

        // Bottom: Configure, Cancel
        var bottomMid = new Vector2(0, m_size.Value.Y / 2);
        btnConfigure = new MyGuiControlButton(
            text: new StringBuilder("Configure"),
            onButtonClick: OnConfigureClick,
            position: new Vector2(bottomMid.X - (GuiSpacing / 2), bottomMid.Y - GuiSpacing),
            originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);

        btnCancel = new MyGuiControlButton(
            text: new StringBuilder("Cancel"),
            onButtonClick: OnCancelClick);

        PositionToRight(btnConfigure, btnCancel);

        Controls.Add(btnConfigure);
        Controls.Add(btnCancel);
        btnConfigure.Enabled = false;
        AddBarAbove(btnConfigure);

        // Table
        RectangleF area = GetAreaBetween(caption, btnConfigure, GuiSpacing * 2);
        table = new MyGuiControlTable()
        {
            Size = area.Size,
            Position = area.Position,
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
        };
        table.ColumnsCount = 1;
        table.SetCustomColumnWidths(new[] { 1.0f });
        table.SetColumnName(0, new StringBuilder("Name"));
        table.SetColumnComparison(0, CellTextComparison);
        table.ItemDoubleClicked += OnItemDoubleClicked;
        table.ItemSelected += OnItemSelected;
        SetTableHeight(table, area.Size.Y);
        AddTableRows();
        table.SortByColumn(0, MyGuiControlTable.SortStateEnum.Ascending);
        Controls.Add(table);
    }

    private void AddTableRows()
    {
        foreach (var p in pluginInstances)
        {
            if (p.HasConfigDialog)
            {
                table.Add(CreateRow(p));
            }
        }
    }

    private int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
    {
        if (x == null)
            return y == null ? 0 : 1;

        return y == null ? -1 : TextComparison(x.Text, y.Text);
    }

    private int TextComparison(StringBuilder x, StringBuilder y)
    {
        if (x == null)
            return y == null ? 0 : 1;

        return y == null ? -1 : x.CompareTo(y);
    }

    private static MyGuiControlTable.Row CreateRow(PluginInstance pluginInstance)
    {
        MyGuiControlTable.Row row = new MyGuiControlTable.Row(pluginInstance);
        row.AddCell(new MyGuiControlTable.Cell(text: pluginInstance.FriendlyName, userData: pluginInstance));
        return row;
    }

    private void OnItemSelected(MyGuiControlTable arg1, MyGuiControlTable.EventArgs arg2)
    {
        selectedPluginInstance = table.SelectedRow?.GetCell(0).UserData as PluginInstance;
        btnConfigure.Enabled = selectedPluginInstance != null;
    }

    private void OnItemDoubleClicked(MyGuiControlTable arg1, MyGuiControlTable.EventArgs arg2)
    {
        selectedPluginInstance?.OpenConfig();
        CloseScreen();
    }

    private void OnConfigureClick(MyGuiControlButton obj)
    {
        selectedPluginInstance?.OpenConfig();
        CloseScreen();
    }

    private void OnCancelClick(MyGuiControlButton obj)
    {
        CloseScreen();
    }
}