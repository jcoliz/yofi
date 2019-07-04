using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManiaLabs.Portable.Base
{
    /// <summary>
    /// Interface to the platform-specific way to interact with Azure Storage
    /// </summary>
    /// <remarks>
    /// The problem is that AzureStorage is platform specific. There is not actually a PCL :(
    /// </remarks>
    public interface IPlatformAzureStorage
    {
        /// <summary>
        /// Do any initial setup
        /// </summary>
        /// <returns></returns>
        void Initialize();

        /// <summary>
        /// Post the Fields to the given TableName
        /// </summary>
        /// <exception cref="System.Exception">
        /// Throws various exceptions for problem situations
        /// </exception>
        /// <param name="TableName">Name of the table</param>
        /// <param name="Fields">Fields and value</param>
        /// <returns>Row key for this item, or null if failed</returns>
        Task<string> PostTableEntry(string TableName, IReadOnlyDictionary<string, string> Fields);

        /// <summary>
        /// Upload the given Stream to the given ContainerName, as the given FileName
        /// </summary>
        /// <param name="ContainerName">Name of the container</param>
        /// <param name="FileName">Name of file</param>
        /// <param name="stream">Stream of data</param>
        /// <returns>Uri to location of blob</returns>
        Task<Uri> UploadToBlob(string ContainerName, string FileName, Stream stream);

        /// <summary>
        /// Check whether the given file exists
        /// </summary>
        /// <param name="ContainerName">Name of the container</param>
        /// <param name="FileName">Name of file</param>
        /// <returns>true if exists</returns>
        Task<bool> DoesBlobExist(string ContainerName, string FileName);

        /// <summary>
        /// Download from the given ContainerName, the given FileName
        /// </summary>
        /// <param name="ContainerName">Name of the container</param>
        /// <param name="FileName">Name of file</param>
        /// <returns></returns>
        Task DownloadBlob(string ContainerName, string FileName, Stream stream);
    }
}
