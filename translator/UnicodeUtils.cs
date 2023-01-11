using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace translator
{
    class UnicodeUtils
    {
        public static string UnicodeDecode(string unicodeStr)
        {
            if (string.IsNullOrWhiteSpace(unicodeStr) || (!unicodeStr.Contains("\\u") && !unicodeStr.Contains("\\U")))
            {
                return unicodeStr;
            }

            string newStr = Regex.Replace(unicodeStr, @"\\[uU](.{4})", (m) =>
            {
                string unicode = m.Groups[1].Value;
                if (int.TryParse(unicode, System.Globalization.NumberStyles.HexNumber, null, out int temp))
                {
                    return ((char)temp).ToString();
                }
                else
                {
                    return m.Groups[0].Value;
                }
            }, RegexOptions.Singleline);

            return newStr;
        }
    }
}
