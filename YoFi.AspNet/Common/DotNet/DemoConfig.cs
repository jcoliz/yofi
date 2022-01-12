namespace Common.DotNet
{
    public class DemoConfig
    {
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
        /// </remarks>
        public bool IsDemo { get; set; }
    }
}
