using avaness.PluginLoader.Data;
using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.GUI
{
    public class MyGuiScreenLocalFolder : MyGuiScreenBase
    {
        private const float Spacing = 0.0175f;

        private MyGuiControlLabel lblFolder;
        private MyGuiControlLabel lblFile;
        private MyGuiControlListbox listFiles;

        private LocalFolderPlugin data;
        private GitHubPlugin github;
        private LocalFolderPlugin.Config settings;
        private MyGuiScreenPluginConfig pluginList;

        public MyGuiScreenLocalFolder(MyGuiScreenPluginConfig pluginList) : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(1f, 0.97f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
        {
            CloseButtonEnabled = true;

            this.pluginList = pluginList;
            settings = new LocalFolderPlugin.Config();
        }

        public MyGuiScreenLocalFolder(MyGuiScreenPluginConfig pluginList, LocalFolderPlugin data) : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(1f, 0.97f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
        {
            CloseButtonEnabled = true;

            this.pluginList = pluginList;
            this.data = data;
            github = data.DataFile;
            settings = data.FolderSettings.Copy();
        }


        public override string GetFriendlyName()
        {
            return "MyGuiScreenLocalFolder";
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            MyGuiControlLabel title = AddCaption("Plugins List");

            // Sets the origin relative to the center of the caption on the X axis and to the bottom the caption on the y axis.
            Vector2 origin = title.Position += new Vector2(0f, title.Size.Y / 2);
            Vector2 size = Size.Value;

            origin.Y += Spacing;

            MyGuiControlButton btnOpenFolder = new MyGuiControlButton(origin, MyGuiControlButtonStyleEnum.Small, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, text: new StringBuilder("Open Folder"), onButtonClick: OnSelectFolder);
            btnOpenFolder.Enabled = data == null;
            Controls.Add(btnOpenFolder);
            origin.Y += btnOpenFolder.Size.Y;

            lblFolder = new MyGuiControlLabel(origin, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            if (settings.Folder != null)
                lblFolder.Text = settings.Folder;
            Controls.Add(lblFolder);
            origin.Y += lblFolder.Size.Y + Spacing;

            MyGuiControlButton btnOpenFile = new MyGuiControlButton(origin, MyGuiControlButtonStyleEnum.Small, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, text: new StringBuilder("Select Data File"), onButtonClick: OnSelectFile);
            Controls.Add(btnOpenFile);
            origin.Y += btnOpenFile.Size.Y;

            lblFile = new MyGuiControlLabel(origin, text: "", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
            if (settings.DataFile != null)
                lblFile.Text = settings.DataFile;
            Controls.Add(lblFile);
            origin.Y += lblFile.Size.Y + Spacing;

            listFiles = new MyGuiControlListbox(origin);
            listFiles.VisibleRowsCount = 7;
            listFiles.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            listFiles.ItemSize = new Vector2(listFiles.ItemSize.X + 0.2f, listFiles.ItemSize.Y);
            listFiles.Size = new Vector2(listFiles.Size.X + 0.2f, listFiles.Size.Y);
            Controls.Add(listFiles);
            origin.Y += listFiles.Size.Y + Spacing;

            MyGuiControlButton btnApply = new MyGuiControlButton(origin, MyGuiControlButtonStyleEnum.Default, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, text: new StringBuilder(data == null ? "Create" : "Apply"), onButtonClick: OnApplyClicked);
            Controls.Add(btnApply);

            UpdateFileList();
        }

        private void OnSelectFile(MyGuiControlButton btn)
        {
            LoaderTools.OpenFileDialog("Open the xml data file", null, "Xml files (*.xml)|*.xml|All files (*.*)|*.*", OnFileSelected);
        }
        private void OnFileSelected(bool hasFile, string path)
        {
            if(hasFile && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    github = GitHubPlugin.DeserializeFile(path);
                    lblFile.Text = path;
                    settings.DataFile = path;
                }
                catch (Exception e)
                {
                    github = null;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("An error occurred while reading the data file:");
                    sb.Append(e);
                    MyGuiSandbox.CreateMessageBox(messageText: sb);
                }
                UpdateFileList();
            }
        }

        private void OnSelectFolder(MyGuiControlButton btn)
        {
            LoaderTools.OpenFolderDialog("Open the root of your project", null, OnFolderSelected);
        }
        private void OnFolderSelected(bool hasFolder, string path)
        {
            if (hasFolder && !string.IsNullOrWhiteSpace(path))
            {
                if (Main.Instance.List.Contains(path))
                {
                    MyGuiSandbox.CreateMessageBox(messageText: new StringBuilder("That folder already exists in the list!"));
                    return;
                }

                lblFolder.Text = path;
                settings.Folder = path;
                UpdateFileList();
            }
        }

        private void UpdateFileList()
        {
            listFiles.ClearItems();

            if (!string.IsNullOrWhiteSpace(settings.Folder) && Directory.Exists(settings.Folder) && github != null)
            {
                int start = settings.Folder.Length + 1;
                foreach (string file in LocalFolderPlugin.GetProjectFiles(settings.Folder, github.SourceDirectories))
                {
                    if (file.Length > start)
                    {
                        StringBuilder text = new StringBuilder(file.Substring(start));
                        var item = new MyGuiControlListbox.Item(text);
                        listFiles.Add(item);
                    }
                }

                if (Application.OpenForms.Count > 0)
                    Application.OpenForms[0].Show(); // TODO figure out why the SE becomes unclickable
            }

        }

        /*private void UpdateFileListThread()
        {
            IEnumerable<string> files = null;
            try
            {
                files = LocalFolderPlugin.GetProjectFiles(settings.Folder, github.SourceDirectories);
            } catch { }
            MySandboxGame.Static.Invoke(() => UpdateFileListAfter(files), "PluginLoader");
        }*/

        private void OnApplyClicked(MyGuiControlButton btn)
        {
            if(data == null)
            {
                data = new LocalFolderPlugin(settings, github);
                Main.Instance.Config.PluginFolders[data.Id] = settings;
                pluginList.CreatePlugin(data);
            }
            else
            {
                if (github != null)
                    data.CopyData(github);
                pluginList.RefreshSidePanel();
            }
        }
    }
}
