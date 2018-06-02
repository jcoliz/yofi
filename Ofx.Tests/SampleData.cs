using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ofx.Tests
{
    public static class SampleData
    {
        public static Stream Open(string filename)
        {
            var assy = Assembly.GetExecutingAssembly();

            var name = assy.GetManifestResourceNames().Where(x => x.Contains(filename)).Single();

            var stream = assy.GetManifestResourceStream(name);

            if (null == stream)
                throw new FileNotFoundException($"{filename} not found in assembly");

            return stream;
        }
    }
}
