namespace avaness.PluginLoader.Tools
{
    public class ItemView
    {
        public readonly string[] Labels;
        public readonly object[] Values;

        public ItemView(string[] labels, object[] values)
        {
            Labels = labels;
            Values = values;
        }
    }
}