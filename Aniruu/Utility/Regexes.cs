using System.Text.RegularExpressions;

namespace Aniruu.Utility;

public partial class Regexes
{
    [GeneratedRegex(@"\s\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase, "en-US")]
    public static partial Regex ExcessSpacing();
}
