using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
using NuGet.Versioning;

namespace avaness.PluginLoader.Network
{
    public class NuGetClient
    {
        const string NugetServiceIndex = "https://api.nuget.org/v3/index.json";
        private string binFolder;
        private string packageFolder;
        private ILogger logger = new NuGetLogger();

        public void Init()
        {
            binFolder = Path.GetFullPath(Path.Combine("NuGet", "bin"));
            Directory.CreateDirectory(binFolder);

            packageFolder = Path.GetFullPath(Path.Combine("NuGet", "packages"));
            Directory.CreateDirectory(packageFolder);

            AppDomain.CurrentDomain.AssemblyResolve += ResolveNuGetPackages;
        }

        private Assembly ResolveNuGetPackages(object sender, ResolveEventArgs args)
        {
            string requestingAssembly = args.RequestingAssembly?.GetName().ToString();
            AssemblyName targetAssembly = new AssemblyName(args.Name);
            string targetPath = Path.Combine(binFolder, targetAssembly.Name + ".dll");
            if (File.Exists(targetPath))
            {
                Assembly a = Assembly.LoadFile(targetPath);
                if (requestingAssembly != null)
                    logger.LogInformation("Resolved " + args.Name + " for " + requestingAssembly);
                else
                    logger.LogInformation("Resolved " + args.Name);
                return a;
            }
            return null;
        }

        public Task InstallPackage(string id, string version, string framework = "net48")
        {
            return InstallPackage(new PackageIdentity(id, NuGetVersion.Parse(version)), framework);
        }

        public async Task InstallPackage(PackageIdentity package, string framework = "net48")
        {
            ISettings settings = Settings.LoadDefaultSettings(root: null);
            SourceRepository sourceRepository = Repository.Factory.GetCoreV3(NugetServiceIndex);
            NuGetFramework nuGetFramework = NuGetFramework.ParseFolder(framework);

            using (SourceCacheContext cacheContext = new SourceCacheContext())
            {
                HashSet<SourcePackageDependencyInfo> availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

                // Get Dependancy tree
                await GetPackageDependencies(package, nuGetFramework, cacheContext, logger, sourceRepository, availablePackages);

                PackageResolverContext resolverContext = new PackageResolverContext(
                    DependencyBehavior.Lowest,
                    new[] { package.Id },
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<PackageReference>(),
                    Enumerable.Empty<PackageIdentity>(),
                    availablePackages,
                    new[] { sourceRepository.PackageSource },
                    logger);

                // Filter and resolve dependencies
                IEnumerable<SourcePackageDependencyInfo> packagesToInstall = new PackageResolver().Resolve(resolverContext, CancellationToken.None)
                    .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));

                PackagePathResolver packagePathResolver = new PackagePathResolver(packageFolder);

                ClientPolicyContext clientPolicyContext = ClientPolicyContext.GetClientPolicy(settings, logger);
                PackageExtractionContext packageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.Skip, clientPolicyContext, logger);


                // Install packages
                foreach (SourcePackageDependencyInfo packageToInstall in packagesToInstall)
                {
                    if (IsSystemPackage(packageToInstall.Id))
                        continue;

                    // Download package if needed
                    string installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                    if (installedPath == null)
                    {
                        DownloadResource downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);

                        DownloadResourceResult downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                            packageToInstall,
                            new PackageDownloadContext(cacheContext),
                            SettingsUtility.GetGlobalPackagesFolder(settings),
                            logger, CancellationToken.None);

                        await PackageExtractor.ExtractPackageAsync(
                            downloadResult.PackageSource,
                            downloadResult.PackageStream,
                            packagePathResolver,
                            packageExtractionContext,
                            CancellationToken.None);

                        // We want to use the PackageFolderReader instead of downloadResult.PackageReader because it wont have any xml files
                        installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                        if (installedPath == null)
                            return; // Error
                    }

                    // Get files
                    CopyFiles(nuGetFramework, installedPath);

                    logger.LogInformation($"Package installed: {packageToInstall.Id}");
                }
            }
        }

        private bool IsSystemPackage(string id)
        {
            return id.StartsWith("System.");
        }

        private void CopyFiles(NuGetFramework targetFramework, string packagePath)
        {
            PackageFolderReader packageReader = new PackageFolderReader(packagePath);
            FrameworkReducer frameworkReducer = new FrameworkReducer();

            IEnumerable<FrameworkSpecificGroup> items = packageReader.GetLibItems();
            NuGetFramework nearest = frameworkReducer.GetNearest(targetFramework, items.Select(x => x.TargetFramework));
            if (nearest != null)
                CopyFiles(packagePath, binFolder, items.Where(x => x.TargetFramework.Equals(nearest)).SelectMany(x => x.Items));

            items = packageReader.GetContentItems();
            nearest = frameworkReducer.GetNearest(targetFramework, items.Select(x => x.TargetFramework));
            if (nearest != null)
                CopyFiles(packagePath, binFolder, items.Where(x => x.TargetFramework.Equals(nearest)).SelectMany(x => x.Items));
            
        }

        private void CopyFiles(string packagePath, string destination, IEnumerable<string> files)
        {
            foreach (string item in files)
            {
                string inputFile = Path.Combine(packagePath, item);
                string outputFile = Path.Combine(destination, Path.GetFileName(item));
                if (!File.Exists(outputFile) || !FilesAreEqual_Hash(inputFile, outputFile))
                {
                    File.Copy(inputFile, outputFile, true);
                    Console.WriteLine("Copied " + item);
                }
                else
                {
                    Console.WriteLine("Skipped " + item);
                }
            }
        }

        private async Task GetPackageDependencies(PackageIdentity package, NuGetFramework framework, SourceCacheContext cacheContext,
            ILogger logger, SourceRepository sourceRepository, ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package))
                return;

            DependencyInfoResource dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
            SourcePackageDependencyInfo dependencyInfo = await dependencyInfoResource.ResolvePackage(
                package, framework, cacheContext, logger, CancellationToken.None);

            if (dependencyInfo == null)
                return;

            availablePackages.Add(dependencyInfo);
            foreach (PackageDependency dependency in dependencyInfo.Dependencies)
            {
                await GetPackageDependencies(
                    new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                    framework, cacheContext, logger, sourceRepository, availablePackages);
            }
        }

        private bool FilesAreEqual_Hash(string first, string second)
        {
            FileInfo firstInfo = new FileInfo(first);
            FileInfo secondInfo = new FileInfo(second);
            if (firstInfo.Length != secondInfo.Length)
                return false;

            MD5 md5 = MD5.Create();

            byte[] firstHash;
            using (FileStream firstStream = firstInfo.OpenRead())
            {
               firstHash = md5.ComputeHash(firstStream);
            }

            byte[] secondHash;
            using (FileStream secondStream = secondInfo.OpenRead())
            {
                secondHash = md5.ComputeHash(secondStream);
            }

            for (int i = 0; i < firstHash.Length; i++)
            {
                if (firstHash[i] != secondHash[i])
                    return false;
            }
            return true;
        }
    }
}
