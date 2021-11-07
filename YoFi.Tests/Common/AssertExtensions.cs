using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Common.DotNet.Test
{
    internal static class AssertExtensions
    {
        public static T IsOfType<T>(this Assert _, object actual) where T : class
        {
            if (actual is T)
                return actual as T;

            throw new AssertFailedException($"Assert.That.IsOfType failed. Expected <{typeof(T).Name}> Actual <{actual.GetType().Name}>");
        }
    }
}
