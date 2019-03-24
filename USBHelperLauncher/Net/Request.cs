using Fiddler;
using System;
using System.Text.RegularExpressions;

namespace USBHelperLauncher.Net
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class Request : Attribute
    {
        private readonly string mask;

        public Request(string mask)
        {
            this.mask = mask;
        }

        public bool Matches(Session oS)
        {
            return FitsMask(oS.PathAndQuery, mask);
        }

        public string GetMask()
        {
            return mask;
        }

        private bool FitsMask(string fileName, string fileMask)
        {
            string pattern =
                 '^' +
                 Regex.Escape(fileMask.Replace(".", "__DOT__")
                                 .Replace("*", "__STAR__")
                                 .Replace("?", "__QM__"))
                     .Replace("__DOT__", "[.]")
                     .Replace("__STAR__", ".*")
                     .Replace("__QM__", ".")
                 + '$';
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(fileName);
        }
    }
}
