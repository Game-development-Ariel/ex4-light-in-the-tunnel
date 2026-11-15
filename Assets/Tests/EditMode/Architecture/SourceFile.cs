using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>One .cs file of the game, read for the architecture tests, with its comments and strings removed.</summary>
public sealed class SourceFile
{
    // Strings and comments are matched together, so "//" inside a string is not taken for a comment.
    private static readonly Regex StringsAndComments = new Regex(
        "@\"(?:\"\"|[^\"])*\"|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])'|//[^\\n]*|/\\*.*?\\*/",
        RegexOptions.Singleline);

    public string Path { get; }
    public string Folder { get; }
    public string Name { get; }
    public string Code { get; }

    private SourceFile(string path, string scriptsRoot)
    {
        Path = path.Replace('\\', '/');
        string relative = Path.Substring(scriptsRoot.Length + 1);
        Folder = relative.Contains("/") ? relative.Substring(0, relative.IndexOf('/')) : "";
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Code = StringsAndComments.Replace(File.ReadAllText(path), match => match.Value.StartsWith("/") ? "" : "\"\"");
    }

    public static SourceFile[] All(string folder)
    {
        string root = (Application.dataPath + "/" + folder).Replace('\\', '/');
        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(path => new SourceFile(path, root))
            .OrderBy(file => file.Path)
            .ToArray();
    }

    /// <summary>The body of a method of this file, from its first brace to the matching one. Empty if there is none.</summary>
    public string MethodBody(string method)
    {
        Match header = Regex.Match(Code, @"\b" + method + @"\s*\([^)]*\)\s*\{");
        if (!header.Success)
        {
            return "";
        }
        int depth = 0;
        int start = header.Index + header.Length - 1;
        for (int i = start; i < Code.Length; i++)
        {
            if (Code[i] == '{')
            {
                depth++;
            }
            else if (Code[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return Code.Substring(start, i - start + 1);
                }
            }
        }
        return "";
    }

    public override string ToString()
    {
        return Path.Substring(Path.IndexOf("Assets/"));
    }
}
