namespace YoFi.Core.Models
{
    /// <summary>
    /// Identifies an object with an ID
    /// </summary>
    /// <remarks>
    /// This is used by the generic controller test helper to generically test
    /// controllers of any model type. The only thing we know about the model
    /// types is that they have an ID.
    /// </remarks>
    public interface IID
    {
        /// <summary>
        /// The ID of the object
        /// </summary>
        int ID { get; set; }
    }
}
