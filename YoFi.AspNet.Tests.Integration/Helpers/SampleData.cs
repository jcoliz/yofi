using System.IO;
using System.Linq;
using System.Reflection;

namespace YoFi.Tests.Integration.Helpers
{
    public static class SampleData
    {
        public static Stream Open(string filename)
        {
            var assy = Assembly.GetExecutingAssembly();
            var names = assy.GetManifestResourceNames();

            var matching = names.Where(x => x.Contains(filename));
            if (!matching.Any())
                throw new FileNotFoundException($"{filename} not found in assembly");
            if (matching.Skip(1).Any())
                throw new FileNotFoundException($"{filename} is ambiguous. Multiple found in assembly");

            var name = matching.First();
            var stream = assy.GetManifestResourceStream(name);
            if (null == stream)
                throw new FileNotFoundException($"{filename} not found in assembly");

            return stream;
        }
    }
}