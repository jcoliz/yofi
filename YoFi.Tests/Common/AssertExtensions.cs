using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YoFi.Tests.Common
{
    internal static class AssertExtensions
    {
        public static T IsOfType<T>(this Assert _, object actual) where T : class
        {
            if (actual is T)
                return actual as T;

            throw new AssertFailedException($"Assert.That.IsOfType failed. Expected <{typeof(T).Name}> Actual <{actual.GetType().Name}>");
        }

        public static void ActionResultOk(this Assert _, IActionResult actionresult)
        {
            var objectresult = actionresult as ObjectResult;
            if (objectresult?.StatusCode == 500)
                throw new AssertFailedException($"Assert.That.ActionResultOk failed <{objectresult.Value as string}>.");
        }
    }
}
