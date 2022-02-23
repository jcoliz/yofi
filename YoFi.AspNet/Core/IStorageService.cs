using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YoFi.Core
{
    /// <summary>
    /// Describes the cloud storage services we require
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Upload this file to blob storage
        /// </summary>
        /// <exception cref="Exception">
        /// Will throw exceptions if encounters problems
        /// </exception>
        /// <param name="filename">Name of the file in storage</param>
        /// <param name="stream">Source of the bits</param>
        /// <param name="contenttype">Type of the content</param>
        /// <returns>Uri where the data is stored</returns>
        Task<Uri> UploadBlobAsync(string filename, Stream stream, string contenttype);

        /// <summary>
        /// Download this file from blob storage
        /// </summary>
        /// <exception cref="Exception">
        /// Will throw exceptions if encounters problems
        /// </exception>
        /// <param name="filename">Name of the file in storage</param>
        /// <param name="stream">Target for the bits</param>
        /// <returns>type of the content</returns>
        Task<string> DownloadBlobAsync(string filename, Stream stream);

        Task<IEnumerable<string>> GetBlobNamesAsync();

        /// <summary>
        /// Name of the blob storage container we store in
        /// </summary>
        public string ContainerName { get; set; }
    }
}
