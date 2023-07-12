using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NuGet;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using VRage.FileSystem;

namespace avaness.PluginLoader.Network
{
    public class NuGetClient
    {
        const string NugetServiceIndex = "https://api.nuget.org/v3/index.json";
        private static readonly ILogger logger = new NuGetLogger();

        private readonly string packageFolder;
        private readonly SourceRepository sourceRepository;
        private readonly PackagePathResolver pathResolver;
        private readonly PackageExtractionContext extractionContext;
        private readonly ISettings nugetSettings;

        public NuGetClient()
        {
            nugetSettings = Settings.LoadDefaultSettings(root: null);
            extractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.Skip, ClientPolicyContext.GetClientPolicy(nugetSettings, logger), logger);
            sourceRepository = Repository.Factory.GetCoreV3(NugetServiceIndex);

            packageFolder = Path.GetFullPath(Path.Combine(MyFileSystem.ExePath, "NuGet", "packages"));
            Directory.CreateDirectory(packageFolder);
            pathResolver = new PackagePathResolver(packageFolder);
        }

        public NuGetPackage[] DownloadFromConfig(Stream packagesConfig)
        {
            return Task.Run(() => DownloadFromConfigAsync(packagesConfig)).GetAwaiter().GetResult();
        }

        public async Task<NuGetPackage[]> DownloadFromConfigAsync(Stream packagesConfig)
        {
            PackagesConfigReader reader = new PackagesConfigReader(packagesConfig, true);
            List<NuGetPackage> packages = new List<NuGetPackage>();
            using (SourceCacheContext cacheContext = new SourceCacheContext())
            {
                foreach (PackageReference package in reader.GetPackages(false))
                {
                    NuGetPackage installedPackage = await DownloadPackage(cacheContext, package.PackageIdentity, package.TargetFramework);
                    if(installedPackage != null)
                        packages.Add(installedPackage);
                }
            }

            return packages.ToArray();
        }

        public NuGetPackage[] DownloadPackages(IEnumerable<NuGetPackageId> packageIds)
        {
            return Task.Run(() => DownloadPackagesAsync(packageIds)).GetAwaiter().GetResult();
        }

        public async Task<NuGetPackage[]> DownloadPackagesAsync(IEnumerable<NuGetPackageId> packageIds)
        {
            List<NuGetPackage> packages = new List<NuGetPackage>();
            using (SourceCacheContext cacheContext = new SourceCacheContext())
            {
                foreach (NuGetPackageId package in packageIds)
                {
                    if(package.TryGetIdentity(out PackageIdentity id))
                    {
                        NuGetPackage installedPackage = await DownloadPackage(cacheContext, id);
                        if (installedPackage != null)
                            packages.Add(installedPackage);
                    }
                }
            }

            return packages.ToArray();
        }

        public async Task<NuGetPackage> DownloadPackage(SourceCacheContext cacheContext, PackageIdentity package, NuGetFramework framework = null)
        {
            if (!IsValidPackage(package.Id))
                return null;

            if (framework == null || framework.IsAny || framework.IsAgnostic || framework.IsUnsupported)
                framework = NuGetFramework.Parse("net48");

            // Download package if needed
            string installedPath = pathResolver.GetInstalledPath(package);
            if (installedPath == null)
            {
                DownloadResource downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>(CancellationToken.None);

                DownloadResourceResult downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                    package,
                    new PackageDownloadContext(cacheContext),
                    SettingsUtility.GetGlobalPackagesFolder(nugetSettings),
                    logger, CancellationToken.None);

                await PackageExtractor.ExtractPackageAsync(
                    downloadResult.PackageSource,
                    downloadResult.PackageStream,
                    pathResolver,
                    extractionContext,
                    CancellationToken.None);

                installedPath = pathResolver.GetInstalledPath(package);
                if (installedPath == null)
                    return null;
            }

            logger.LogInformation($"Package downloaded: {package.Id}");
            return new NuGetPackage(installedPath, framework);
        }

        private bool IsValidPackage(string id)
        {
            return !id.StartsWith("System.") && id != "Lib.Harmony";
        }

    }
}
