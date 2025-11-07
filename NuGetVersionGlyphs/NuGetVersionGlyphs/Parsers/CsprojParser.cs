using System.Collections.Generic;
using System.Text.RegularExpressions;
using NuGetVersionGlyphs.Models;

namespace NuGetVersionGlyphs.Parsers
{
    public class CsprojParser
    {
        private static readonly Regex PackageReferenceRegex = new Regex(
            @"<PackageReference\s+Include\s*=\s*""(?<packageId>[^""]+)""\s+Version\s*=\s*""(?<version>[^""]+)""\s*/>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<PackageReferenceInfo> ParsePackageReferences(string text)
        {
            var packages = new List<PackageReferenceInfo>();
            var lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var match = PackageReferenceRegex.Match(line);

                if (match.Success)
                {
                    var packageId = match.Groups["packageId"].Value;
                    var version = match.Groups["version"].Value;

                    packages.Add(new PackageReferenceInfo
                    {
                        PackageId = packageId,
                        CurrentVersion = version,
                        LineNumber = i,
                        StartPosition = 0,
                        EndPosition = line.Length
                    });
                }
            }

            return packages;
        }
    }
}
