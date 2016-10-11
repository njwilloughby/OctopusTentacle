﻿using System.IO;
using System.Linq;
using System.Threading;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using Octopus.Server.Extensibility.HostServices.Diagnostics;
using Octopus.Shared.Util;

namespace Octopus.Shared.Packages
{
    public class NuGetPackageInstaller : IPackageInstaller
    {
        readonly IOctopusFileSystem fileSystem;

        public NuGetPackageInstaller(IOctopusFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public int Install(string packageFile, string directory, ILog log, bool suppressNestedScriptWarning)
        {
            using (var packageStream = fileSystem.OpenFile(packageFile, FileMode.Open, FileAccess.Read))
                return Install(packageStream, directory, log, suppressNestedScriptWarning);
        }

        public int Install(Stream packageStream, string directory, ILog log, bool suppressNestedScriptWarning)
        {
            var extracted = PackageExtractor.ExtractPackage(packageStream, new SuppliedDirectoryPackagePathResolver(directory),
                new PackageExtractionContext {PackageSaveMode = PackageSaveMode.Files, XmlDocFileSaveMode = XmlDocFileSaveMode.None, CopySatelliteFiles = false},
                CancellationToken.None);

            return extracted.Count();
        }

        class SuppliedDirectoryPackagePathResolver : PackagePathResolver
        {
            public SuppliedDirectoryPackagePathResolver(string packageDirectory) : base(packageDirectory, false)
            {
            }

            public override string GetInstallPath(PackageIdentity packageIdentity)
            {
                return Root;
            }
        }
    }
}