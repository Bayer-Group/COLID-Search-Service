using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace COLID.SearchService.Repositories.Mapping.Extensions
{
    public static class StringExtension
    {
        public static string GetPreparedAnalyzerName(this string str)
        {
            return Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out var uri) ? HttpUtility.UrlEncode(uri.PathAndQuery) : str;
        }

        public static string GetPreparedAnalyzerName(this string str, string prefix)
        {
            return string.Format(prefix, GetPreparedAnalyzerName(str));
        }
    }
}
