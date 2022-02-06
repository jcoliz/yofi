using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoFi.Tests.Integration.Helpers
{

    // https://code-maze.com/aspnet-core-testing-anti-forgery-token/
    public static class AntiForgeryTokenExtractor
    {
        public static string AntiForgeryFieldName { get; } = "__RequestVerificationToken";
        public static string AntiForgeryCookieName { get; } = "AspNetCore.Antiforgery";

        public static CookieHeaderValue ExtractAntiForgeryCookieValueFrom(HttpResponseMessage response)
        {
            var antiForgeryCookie = response.Headers.GetValues("Set-Cookie")
                .FirstOrDefault(x => x.Contains(AntiForgeryCookieName));

            if (antiForgeryCookie is null)
                throw new ArgumentException($"Cookie '{AntiForgeryCookieName}' not found in HTTP response", nameof(response));

            var setcookie = SetCookieHeaderValue.Parse(antiForgeryCookie);

            var cookie = new CookieHeaderValue(setcookie.Name, setcookie.Value);

            return cookie;
        }

        public static KeyValuePair<string, string> ExtractAntiForgeryToken(IHtmlDocument document)
        {
            var found = document.QuerySelector($"input[name={AntiForgeryFieldName}]");

            if (found is null)
                throw new ArgumentException($"Anti forgery token '{AntiForgeryFieldName}' not found in HTML", nameof(document));

            var value = found.GetAttribute("value");

            if (value is null)
                throw new ArgumentException($"Anti forgery token '{AntiForgeryFieldName}' does not have a value attribute", nameof(document));

            return new KeyValuePair<string, string>(AntiForgeryFieldName, value);
        }

    }
}
