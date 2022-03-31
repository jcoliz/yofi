namespace Common.DotNet
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CodebaseConfig
    {
        public const string Section = "Codebase";

        public string Name { get; set; }
        public string IssuesLink { get; set; }
        public string License { get; set; }
        public string LicenseLink { get; set; }
        public string Link { get; set; }
        public string Tagline { get; set; }
        public string Release { get; set; }
    }
}
