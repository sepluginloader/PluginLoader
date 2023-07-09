using NuGet.Frameworks;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace avaness.PluginLoader.Network
{
    public class NuGetPackage
    {
        private readonly string installPath;
        private readonly NuGetFramework targetFramework;

        public Item[] LibFiles { get; private set; }
        public Item[] ContentFiles { get; private set; }

        public NuGetPackage(string installPath, NuGetFramework targetFramework)
        {
            this.installPath = installPath;
            this.targetFramework = targetFramework;
            GetFileLists();
        }

        private void GetFileLists()
        {
            PackageFolderReader packageReader = new PackageFolderReader(installPath);
            FrameworkReducer frameworkReducer = new FrameworkReducer();

            IEnumerable<FrameworkSpecificGroup> items = packageReader.GetLibItems();
            NuGetFramework nearest = frameworkReducer.GetNearest(targetFramework, items.Select(x => x.TargetFramework));
            if (nearest != null)
            {
                FrameworkSpecificGroup group = items.First(x => x.TargetFramework.Equals(nearest));
                LibFiles = group.Items.Select(x => GetPackageItem(x, group.TargetFramework, false)).Where(x => x != null).ToArray();
            }
            else
            {
                LibFiles = Array.Empty<Item>();
            }

            items = packageReader.GetContentItems();
            nearest = frameworkReducer.GetNearest(targetFramework, items.Select(x => x.TargetFramework));
            if (nearest != null)
            {
                FrameworkSpecificGroup group = items.First(x => x.TargetFramework.Equals(nearest));
                ContentFiles = group.Items.Select(x => GetPackageItem(x, group.TargetFramework, true)).Where(x => x != null).ToArray();
            }
            else
            {
                ContentFiles = Array.Empty<Item>();
            }
        }

        private Item GetPackageItem(string path, NuGetFramework framework, bool content)
        {
            string fullPath = Path.GetFullPath(Path.Combine(installPath, path));
            if (!File.Exists(fullPath))
                return null;

            string folder;
            string file;
            if (TrySplitPath(fullPath, framework.GetShortFolderName(), out folder, out file))
                return new Item(file, folder);

            if (TrySplitPath(fullPath, content ? "content" : "lib", out folder, out file))
                return new Item(file, folder);

            return null;
        }

        private bool TrySplitPath(string fullPath, string lastFolderName, out string folder, out string file)
        {
            folder = null;
            file = null;

            int index = fullPath.IndexOf(lastFolderName);
            if (index < 0 || fullPath.Length <= index + lastFolderName.Length + 2)
                return false;

            folder = fullPath.Substring(0, index + lastFolderName.Length);
            file = fullPath.Substring(folder.Length + 1);
            return true;
        }


        public class Item
        {
            public Item(string path, string folder)
            {
                FilePath = path;
                Folder = folder;
                FullPath = Path.Combine(Folder, FilePath);
            }

            public string FilePath { get; set; }
            public string Folder { get; set; }
            public string FullPath { get; set; }

        }
    }
}
