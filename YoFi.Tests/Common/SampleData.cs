using System.IO;
using System.Linq;
using System.Reflection;

namespace Common.NET.Test
{
    public static class SampleData
    {
        public static Stream Open(string filename)
        {
            var assy = Assembly.GetExecutingAssembly();
            var names = assy.GetManifestResourceNames();

            var name = names.Where(x => x.Contains(filename)).Single();

            var stream = assy.GetManifestResourceStream(name);

            if (null == stream)
                throw new FileNotFoundException($"{filename} not found in assembly");

            return stream;
        }
    }
}
