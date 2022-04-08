using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common.DotNet.Test
{
    public static class DeepCopy
    {
        /// <summary>
        /// Make an exact duplicate of these <paramref name="items"/>
        /// </summary>
        /// <param name="items">Items to copy</param>
        /// <returns>List of cloned items</returns>
        public static async Task<List<T>> MakeDuplicateOf<T>(IEnumerable<T> items)
        {
            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, items);
            stream.Seek(0, SeekOrigin.Begin);
            var result = await JsonSerializer.DeserializeAsync<List<T>>(stream);
            return result;
        }
    }
}
