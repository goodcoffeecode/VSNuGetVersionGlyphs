namespace NuGetVersionGlyphs.Models
{
    public class PackageReferenceInfo
    {
        public string PackageId { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public int LineNumber { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public bool IsUpToDate { get; set; }
        public bool IsCurrentVersionPrerelease { get; set; }
    }
}
