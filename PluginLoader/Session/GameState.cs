using Sandbox.Game;

namespace avaness.PluginLoader.Session
{
    public class GameState
    {
        MyGUISettings gui;

        /// <summary>
        /// A collection of members that are commonly modified by plugins.
        /// TODO: Add more to this class.
        /// </summary>
        public GameState()
        {
            gui = MyPerGameSettings.GUI;
        }

        public void Apply()
        {
            MyPerGameSettings.GUI = gui;
        }
    }
}
