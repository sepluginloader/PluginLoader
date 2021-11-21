using Sandbox.Engine.Networking;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using ParallelTasks;
using VRage.Game;
using System.Threading;
using VRage.Utils;
using HarmonyLib;
using System.Reflection;
using System.Text;
using VRage.GameServices;

namespace avaness.PluginLoader
{
    public static class SteamAPI
    {
        private static MethodInfo DownloadModsBlocking;

        public static bool IsSubscribed(ulong id)
        {
            EItemState state = (EItemState)SteamUGC.GetItemState(new PublishedFileId_t(id));
            return (state & EItemState.k_EItemStateSubscribed) == EItemState.k_EItemStateSubscribed;
        }

        public static void SubscribeToItem(ulong id)
        {
            SteamUGC.SubscribeItem(new PublishedFileId_t(id));
        }

        public static void Update(IEnumerable<ulong> ids)
        {
            if (!ids.Any())
                return;

            var modItems = new List<MyObjectBuilder_Checkpoint.ModItem>(ids.Select(x => new MyObjectBuilder_Checkpoint.ModItem(x, "Steam")));
            LogFile.WriteLine($"Updating {modItems.Count} workshop items");

            // Source: MyWorkshop.DownloadWorldModsBlocking
            MyWorkshop.ResultData result = new MyWorkshop.ResultData();
            Task task = Parallel.Start(delegate
            {
                result = UpdateInternal(modItems);
            });
            while (!task.IsComplete)
            {
                MyGameService.Update();
                Thread.Sleep(10);
            }

            if (!result.Success)
            {
                Exception[] exceptions = task.Exceptions;
                if(exceptions != null && exceptions.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("An error occurred while updating workshop items:");
                    foreach (Exception e in exceptions)
                        sb.Append(e);
                    LogFile.WriteLine(sb.ToString());
                }
                else
                {
                    LogFile.WriteLine("Unable to update workshop items");
                }

            }
        }

		public static MyWorkshop.ResultData UpdateInternal(List<MyObjectBuilder_Checkpoint.ModItem> mods)
		{
			// Source: MyWorkshop.DownloadWorldModsBlockingInternal

			MyLog.Default.IncreaseIndent();

            List<WorkshopId> list = new List<WorkshopId>(mods.Select(x => new WorkshopId(x.PublishedFileId, x.PublishedServiceName)));

            if (DownloadModsBlocking == null)
                DownloadModsBlocking = AccessTools.Method(typeof(MyWorkshop), "DownloadModsBlocking");

            MyWorkshop.ResultData resultData = (MyWorkshop.ResultData)DownloadModsBlocking.Invoke(mods, new object[] { 
                mods, new MyWorkshop.ResultData() { Success = true }, list, new MyWorkshop.CancelToken() 
            });

            MyLog.Default.DecreaseIndent();
			return resultData;
		}

        public static List<MyWorkshopItem> ResolveDependencies(IEnumerable<ulong> workshopIds)
        {
            List<MyWorkshopItem> ret = null;
            Parallel.Start(delegate
            {
                ret = MyWorkshop.GetModsDependencyHiearchy(new HashSet<WorkshopId>(workshopIds.Select(b => new WorkshopId(b, MyGameService.GetDefaultUGC().ServiceName))), out _)
                    .Distinct(WorkshopItemIdComparer.Instance).ToList();
            });
            while (ret == null)
            {
                MyGameService.Update();
                Thread.Sleep(10);
            }
            return ret;
        }

        private class WorkshopItemIdComparer : IEqualityComparer<MyWorkshopItem>
        {
            public static readonly WorkshopItemIdComparer Instance = new WorkshopItemIdComparer();
            public bool Equals(MyWorkshopItem x, MyWorkshopItem y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null))
                    return false;
                if (ReferenceEquals(y, null))
                    return false;
                if (x.GetType() != y.GetType())
                    return false;

                return x.Title == y.Title && x.ItemType == y.ItemType && x.Id == y.Id && x.OwnerId == y.OwnerId && x.ServiceName == y.ServiceName;
            }

            public int GetHashCode(MyWorkshopItem obj)
            {
                unchecked
                {
                    var hashCode = (obj.Title != null ? obj.Title.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)obj.ItemType;
                    hashCode = (hashCode * 397) ^ obj.Id.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.OwnerId.GetHashCode();
                    hashCode = (hashCode * 397) ^ (obj.ServiceName != null ? obj.ServiceName.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
	}
}
