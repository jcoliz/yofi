namespace Common.DotNet
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DemoConfig
    {
        public const string Section = "Demo";

        /// <summary>
        /// Whether we're running in a mode where unauthenticated visitors can read data
        /// </summary>
        /// <remarks>
        /// Note that this setting doesn't control it. Only indicates it for places where we should notify the user this is happening
        /// </remarks>
        public bool IsOpenAccess { get; set; }
        /// <summary>
        /// Whether we're running a demo
        /// </summary>
        /// <remarks>
        /// This is used for a lot of things
        ///   * Seed a year's worth of fake data
        ///   * Pop up help messages when first entering most top-level pages
        ///   * "Try the demo" on the home page, vs "Get started"
        ///   * Including extra help text in the combined help page
        ///   * "/" goes to Home instead of Transactions
        /// </remarks>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Whether the "/Home" page should be shown for the root,
        /// else it will go to Transactions
        /// </summary>
        public bool IsHomePageRoot { get; set; } = true;
    }
}
