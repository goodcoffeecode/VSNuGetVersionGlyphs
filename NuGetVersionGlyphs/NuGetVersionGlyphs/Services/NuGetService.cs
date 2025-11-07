using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGetVersionGlyphs.Services
{
    public class NuGetService
    {
        private readonly SourceCacheContext _cache;
        private readonly SourceRepository _repository;
        private readonly ILogger _logger;

        public NuGetService()
        {
            _cache = new SourceCacheContext();
            _logger = NullLogger.Instance;
            _repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        }

        public async Task<IEnumerable<NuGetVersion>> GetPackageVersionsAsync(string packageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
                var versions = await resource.GetAllVersionsAsync(packageId, _cache, _logger, cancellationToken);
                return versions?.OrderByDescending(v => v) ?? Enumerable.Empty<NuGetVersion>();
            }
            catch (Exception)
            {
                return Enumerable.Empty<NuGetVersion>();
            }
        }

        public async Task<NuGetVersion> GetLatestVersionAsync(string packageId, CancellationToken cancellationToken = default)
        {
            var versions = await GetPackageVersionsAsync(packageId, cancellationToken);
            return versions.FirstOrDefault();
        }

        public async Task<List<NuGetVersion>> GetVersionsAroundAsync(string packageId, string currentVersion, int countAbove = 5, int countBelow = 5, CancellationToken cancellationToken = default)
        {
            var allVersions = (await GetPackageVersionsAsync(packageId, cancellationToken)).ToList();
            
            if (!allVersions.Any())
                return new List<NuGetVersion>();

            if (!NuGetVersion.TryParse(currentVersion, out var current))
                return allVersions.Take(countAbove + countBelow + 1).ToList();

            var currentIndex = allVersions.FindIndex(v => v == current);
            
            if (currentIndex == -1)
            {
                // Current version not found, return latest versions
                return allVersions.Take(countAbove + countBelow + 1).ToList();
            }

            var startIndex = Math.Max(0, currentIndex - countAbove);
            var endIndex = Math.Min(allVersions.Count - 1, currentIndex + countBelow);
            var count = endIndex - startIndex + 1;

            return allVersions.GetRange(startIndex, count);
        }
    }
}
