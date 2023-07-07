using System.Text.RegularExpressions;

namespace dymaptic.Chat.Server.Logging;

public static class StringExtensions
{
    /// <summary>
    ///     Replaces just the first character of a string with the lowercase version of the same character.
    /// </summary>
    public static string ToLowerFirstChar(this string input)
    {
        if (input.Length == 1) return new string(char.ToLower(input[0]), 1);

        return string.Create(input.Length, input, (span, txt) =>
        {
            span[0] = char.ToLower(txt[0]);
            txt[1..].CopyTo(span[1..]);
        });
    }

    /// <summary>
    ///     Cleans a string to remove characters that would fail in a file name, replacing them with underscores.
    /// </summary>
    public static string SafeFileName(this string input)
    {
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return Regex.Replace(input, invalidRegStr, "_");
    }

    /// <summary>
    ///     Checks a string filepath, if it is relative, adds it to <see cref="Environment.CurrentDirectory" />,
    ///     otherwise returns the original path. Also creates the directory if it doesn't exist.
    /// </summary>
    public static string CreateOrReturnFullyQualifiedPath(this string path)
    {
        string fullPath = Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(Environment.CurrentDirectory, path);
        Directory.CreateDirectory(fullPath);

        return fullPath;
    }
}