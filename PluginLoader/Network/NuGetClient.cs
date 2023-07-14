using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using VRage.FileSystem;

namespace avaness.PluginLoader.Network
{
    public class NuGetClient
    {
        const string NugetServiceIndex = "https://api.nuget.org/v3/index.json";
        private static readonly NuGetFramework ProjectFramework = NuGetFramework.Parse("net48");

        private static HashSet<string> binAssemblies;
        private static readonly ILogger logger = new NuGetLogger();

        private readonly string packageFolder;
        private readonly SourceRepository sourceRepository;
        private readonly PackagePathResolver pathResolver;
        private readonly PackageExtractionContext extractionContext;
        private readonly ISettings nugetSettings;

        public NuGetClient()
        {
            if(binAssemblies == null)
            {
                binAssemblies = new HashSet<string>(
                    Directory.EnumerateFiles(MyFileSystem.ExePath, "*.dll", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Select(x => x.ToLowerInvariant()));
            }

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
            logger.LogInformation("Downloading packages from packages.config");

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

        public NuGetPackage[] DownloadPackages(IEnumerable<NuGetPackageId> packageIds, bool getDependencies = true)
        {
            return Task.Run(() => DownloadPackagesAsync(packageIds, getDependencies)).GetAwaiter().GetResult();
        }

        public async Task<NuGetPackage[]> DownloadPackagesAsync(IEnumerable<NuGetPackageId> packageIds, bool getDependencies = true)
        {
            List<PackageIdentity> packages = new List<PackageIdentity>();
            foreach(NuGetPackageId id in packageIds)
            {
                if(id.TryGetIdentity(out PackageIdentity nugetId))
                    packages.Add(nugetId);
            }

            if (packages.Count == 0)
                return Array.Empty<NuGetPackage>();

            logger.LogInformation($"Downloading {packages.Count} packages with dependencies");

            List<NuGetPackage> result = new List<NuGetPackage>();
            using (SourceCacheContext cacheContext = new SourceCacheContext())
            {
                IEnumerable<PackageIdentity> downloadPackages = packages.Where(x => !IsSystemPackage(x.Id));
                if (getDependencies)
                    downloadPackages = await ResolveDependencies(downloadPackages, cacheContext);

                foreach (PackageIdentity id in downloadPackages)
                {
                    NuGetPackage installedPackage = await DownloadPackage(cacheContext, id);
                    if (installedPackage != null)
                        result.Add(installedPackage);
                }
            }

            return result.ToArray();
        }

        private async Task<IEnumerable<PackageIdentity>> ResolveDependencies(IEnumerable<PackageIdentity> packages, SourceCacheContext context)
        {
            PackageResolverContext resolverContext = new PackageResolverContext(
                dependencyBehavior: DependencyBehavior.Lowest,
                targetIds: packages.Select(x => x.Id),
                requiredPackageIds: Enumerable.Empty<string>(),
                packagesConfig: Enumerable.Empty<PackageReference>(),
                preferredVersions: Enumerable.Empty<PackageIdentity>(),
                availablePackages: await GetDependencies(packages, context),
                packageSources: new[] { sourceRepository.PackageSource },
                log: logger);

            return new PackageResolver().Resolve(resolverContext, CancellationToken.None);
        }

        private async Task<IEnumerable<SourcePackageDependencyInfo>> GetDependencies(
            IEnumerable<PackageIdentity> packages,
            SourceCacheContext context)
        {
            Dictionary<PackageIdentity, SourcePackageDependencyInfo> result = new Dictionary<PackageIdentity, SourcePackageDependencyInfo>();

            DependencyInfoResource dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
            if (dependencyInfoResource == null)
                return result.Values;

            Stack<PackageIdentity> stack = new Stack<PackageIdentity>(packages);
            while (stack.Count > 0)
            {
                PackageIdentity package = stack.Pop();

                if (!result.ContainsKey(package))
                {
                    SourcePackageDependencyInfo dependencyInfo = await dependencyInfoResource.ResolvePackage(package, ProjectFramework, context, logger, CancellationToken.None);
                    result.Add(package, dependencyInfo);
                    if (dependencyInfo == null)
                        continue;
                    foreach (PackageDependency dependency in dependencyInfo.Dependencies)
                        stack.Push(new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion));
                }
            }

            return result.Values.Where(x => x != null);
        }

        public async Task<NuGetPackage> DownloadPackage(SourceCacheContext cacheContext, PackageIdentity package, NuGetFramework framework = null)
        {
            if (IsSystemPackage(package.Id))
                return null;

            if (framework == null || framework.IsAny || framework.IsAgnostic || framework.IsUnsupported)
                framework = ProjectFramework;

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

                logger.LogInformation($"Package downloaded: {package.Id}");
            }
            else
            {
                logger.LogInformation($"Package already exists: {package.Id}");
            }

            return new NuGetPackage(installedPath, framework);
        }

        private bool IsSystemPackage(string id)
        {
            return id.Equals("Lib.Harmony", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith("System", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith("Microsoft.NET", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith("NETStandard", StringComparison.InvariantCultureIgnoreCase) ||
                id.StartsWith("Microsoft.Win32", StringComparison.InvariantCultureIgnoreCase) ||
                binAssemblies.Contains(id.ToLowerInvariant());
        }

    }
}
