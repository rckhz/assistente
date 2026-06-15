using System.Text.RegularExpressions;
using the_bolotas.Models;

namespace the_bolotas.Tools;

public static class BuildErrorParser
{
    public static BuildError? Parse(string buildOutput)
    {
        var pattern =
            @"(?<file>[A-Z]:\\.*?\.\w+)\((?<line>\d+),\d+\): error (?<code>\w+): (?<msg>.*)";

        var match = Regex.Match(
            buildOutput,
            pattern,
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        return new BuildError
        {
            File = match.Groups["file"].Value,
            Line = int.Parse(match.Groups["line"].Value),
            Code = match.Groups["code"].Value,
            Message = match.Groups["msg"].Value
        };
    }
}