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
            var modItems = new List<MyObjectBuilder_Checkpoint.ModItem>(ids.Select(x => new MyObjectBuilder_Checkpoint.ModItem(x, "Steam")));
            if (modItems.Count == 0)
                return;
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

            if (result.Result != VRage.GameServices.MyGameServiceCallResult.OK)
            {
                Exception[] exceptions = task.Exceptions;
                if(exceptions != null && exceptions.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("An error occurred while updating workshop items:");
                    foreach (Exception e in exceptions)
                        sb.Append(e);
                    LogFile.Error(sb.ToString());
                }
                else
                {
                    LogFile.Error("Unable to update workshop items. Result: " + result.Result);
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
                mods, new MyWorkshop.ResultData(), list, new MyWorkshop.CancelToken() 
            });

            MyLog.Default.DecreaseIndent();
			return resultData;
		}

	}
}
