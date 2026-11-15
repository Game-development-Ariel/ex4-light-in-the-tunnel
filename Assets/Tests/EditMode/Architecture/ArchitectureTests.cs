using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The rules of the course and of the README, checked on the code itself, so a change that breaks one fails here.
/// </summary>
public class ArchitectureTests
{
    // The folders of game logic. UI, Effects, Audio and Camera only show and play what the game does.
    private static readonly string[] GameplayFolders = { "Player", "Security", "Items", "Level", "Game", "Events" };

    // Static state the README explains in its statics table. Anything else static and changeable is a new singleton.
    private static readonly HashSet<string> AllowedStaticFields = new HashSet<string> { "Sfx.source", "ScreenFader.isFading" };

    private static readonly HashSet<string> AllowedStaticProperties = new HashSet<string>
    {
        "MobileJoystick.UsesTouch", "UiTheme.TitleFont", "UiTheme.ItalicFont", "UiTheme.ThoughtFont",
    };

    // Tuning numbers without a Tooltip, Range or Min. Keep this list short: a number needs to say what it means.
    private static readonly HashSet<string> UndocumentedNumbers = new HashSet<string> { "CameraIndicators.humVolume", "HeardSteps.maxAlpha" };

    private static readonly Assembly Game = typeof(GameEvent).Assembly;

    private static SourceFile[] scripts;

    private static SourceFile[] Scripts => scripts ??= SourceFile.All("Scripts");

    private static IEnumerable<SourceFile> Runtime => Scripts.Where(file => file.Folder != "Editor");

    private static string Readme => File.ReadAllText(Path.Combine(Application.dataPath, "..", "README.md"));

    private static string Fail(List<string> problems)
    {
        return string.Join("\n", problems);
    }

    [Test]
    public void TheScriptsFolders_AreTheOnesTheReadmeLists()
    {
        string[] folders = Scripts.Select(file => file.Folder).Distinct().OrderBy(f => f).ToArray();
        CollectionAssert.AreEquivalent(
            new[] { "Audio", "Camera", "Editor", "Effects", "Events", "Game", "Items", "Level", "Player", "Security", "UI" }, folders);
    }

    // Rule: the game doesn't know the UI. The UI listens to the game's events, never the other way.
    [Test]
    public void GameCode_DoesNotUseUiClasses()
    {
        string[] uiClasses = Scripts.Where(file => file.Folder == "UI").Select(file => file.Name).ToArray();
        var problems = new List<string>();
        foreach (SourceFile file in Runtime.Where(file => file.Folder != "UI"))
        {
            foreach (string ui in uiClasses.Where(ui => Regex.IsMatch(file.Code, @"\b" + ui + @"\b")))
            {
                problems.Add($"{file} uses the UI class {ui}");
            }
            if (Regex.IsMatch(file.Code, @"using\s+UnityEngine\.UI\s*;"))
            {
                problems.Add($"{file} uses UnityEngine.UI");
            }
        }
        Assert.IsEmpty(problems, Fail(problems));
    }

    // Rule from the README: every subscription is made in OnEnable and undone in OnDisable.
    [Test]
    public void EverySubscription_IsMadeInOnEnable_AndUndoneInOnDisable()
    {
        var problems = new List<string>();
        foreach (SourceFile file in Runtime.Where(file => file.Folder != "Events"))
        {
            string onEnable = file.MethodBody("OnEnable");
            string onDisable = file.MethodBody("OnDisable");
            foreach (Match subscribe in Regex.Matches(file.Code, @"([\w\.]+)\.Subscribe\((\w+)\)"))
            {
                Check(file, subscribe.Value, $"{subscribe.Groups[1].Value}.Unsubscribe({subscribe.Groups[2].Value})", onEnable, onDisable, problems);
            }
            foreach (Match add in Regex.Matches(file.Code, @"([\w\.]+)\s*\+=\s*(\w+)\s*;"))
            {
                string handler = add.Groups[2].Value;
                bool isMethod = Regex.IsMatch(file.Code, @"\b(void|IEnumerator)\s+" + handler + @"\s*\(");
                if (isMethod)
                {
                    Check(file, add.Value.TrimEnd(';'), $"{add.Groups[1].Value} -= {handler}", onEnable, onDisable, problems);
                }
            }
        }
        Assert.IsEmpty(problems, Fail(problems));
    }

    private static void Check(SourceFile file, string subscription, string undo, string onEnable, string onDisable, List<string> problems)
    {
        if (!onEnable.Contains(subscription))
        {
            problems.Add($"{file}: '{subscription}' is not in OnEnable");
        }
        if (!Regex.IsMatch(onDisable, Regex.Escape(undo).Replace(@"\ ", @"\s*")))
        {
            problems.Add($"{file}: '{subscription}' has no '{undo}' in OnDisable");
        }
    }

    // Rule from the README: there is no singleton, and every static that holds state is in the README's table.
    [Test]
    public void NoSingletons_AndEveryStaticStateIsAllowed()
    {
        var problems = new List<string>();
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (Type type in Game.GetTypes().Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)))
        {
            foreach (FieldInfo field in type.GetFields(statics).Where(f => !f.IsLiteral && !f.IsInitOnly))
            {
                string name = $"{type.Name}.{field.Name}";
                if (!AllowedStaticFields.Contains(name))
                {
                    problems.Add($"static field {name} holds state. Make it a scene component, or add it to the README's statics table and to this test");
                }
            }
            foreach (PropertyInfo property in type.GetProperties(statics))
            {
                string name = $"{type.Name}.{property.Name}";
                if (!AllowedStaticProperties.Contains(name))
                {
                    problems.Add($"static property {name} is not in the allowed list");
                }
            }
            foreach (MemberInfo member in type.GetMembers(statics))
            {
                Type memberType = (member as FieldInfo)?.FieldType ?? (member as PropertyInfo)?.PropertyType;
                if (memberType == type || member.Name == "Instance")
                {
                    problems.Add($"{type.Name}.{member.Name} looks like a singleton");
                }
            }
        }
        Assert.IsEmpty(problems, Fail(problems));
    }

    [Test]
    public void EveryClassWithStaticState_IsInTheReadmesStaticsTable()
    {
        string readme = Readme;
        int start = readme.IndexOf("## סינגלטון ומשתנים סטטיים", StringComparison.Ordinal);
        Assert.GreaterOrEqual(start, 0, "The README lost its statics section");
        int end = readme.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        string table = end < 0 ? readme.Substring(start) : readme.Substring(start, end - start);
        foreach (string name in AllowedStaticFields.Concat(AllowedStaticProperties))
        {
            string owner = name.Split('.')[0];
            StringAssert.Contains(owner, table, $"{owner} has static state but is not in the README's statics table");
        }
        StringAssert.Contains("UsesTouch", table);
        StringAssert.Contains("isFading", table);
    }

    // With domain reload off a static keeps its value between runs, so plain static values must be reset at the start.
    [Test]
    public void StaticValues_AreResetAtTheStartOfEveryRun()
    {
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (Type type in Game.GetTypes())
        {
            bool hasPlainState = type.GetFields(statics)
                .Any(f => !f.IsLiteral && !f.IsInitOnly && !typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType));
            if (!hasPlainState)
            {
                continue;
            }
            bool resets = type.GetMethods(statics).Any(m => m.IsDefined(typeof(RuntimeInitializeOnLoadMethodAttribute), false));
            Assert.IsTrue(resets, $"{type.Name} has static values but no [RuntimeInitializeOnLoadMethod] that resets them");
        }
    }

    // Unity needs a MonoBehaviour's file to be named like the class, and one type per file keeps the code easy to find.
    [Test]
    public void EveryFile_HasOneTopLevelType_NamedLikeTheFile()
    {
        var problems = new List<string>();
        var topLevel = new Regex(@"^(?:public\s+|internal\s+)?(?:static\s+|sealed\s+|abstract\s+)*(?:class|struct|interface|enum)\s+(\w+)", RegexOptions.Multiline);
        foreach (SourceFile file in Scripts.Concat(SourceFile.All("Tests")))
        {
            string[] types = topLevel.Matches(file.Code).Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            if (types.Length != 1 || types[0] != file.Name)
            {
                problems.Add($"{file} defines [{string.Join(", ", types)}]");
            }
        }
        Assert.IsEmpty(problems, Fail(problems));
    }

    // Every tuning number in the game logic says what it means in the Inspector.
    [Test]
    public void EveryTuningNumber_InGameCode_HasATooltipOrALimit()
    {
        var problems = new List<string>();
        var field = new Regex(@"(?:\[SerializeField\]\s*private|public)\s+(?:float|int)\s+(\w+)\s*(?:=(?!>)|;)");
        foreach (SourceFile file in Runtime.Where(file => GameplayFolders.Contains(file.Folder)))
        {
            foreach (Match match in field.Matches(file.Code))
            {
                int previousEnd = file.Code.LastIndexOfAny(new[] { ';', '{', '}' }, match.Index - 1);
                string attributes = file.Code.Substring(previousEnd + 1, match.Index - previousEnd - 1) + match.Value;
                bool explained = attributes.Contains("[Tooltip(") || attributes.Contains("[Range(") || attributes.Contains("[Min(");
                string name = $"{file.Name}.{match.Groups[1].Value}";
                if (!explained && !UndocumentedNumbers.Contains(name))
                {
                    problems.Add($"{name} has no [Tooltip], [Range] or [Min]");
                }
            }
        }
        Assert.IsEmpty(problems, Fail(problems));
    }

    // The README is the map of the code. A class it doesn't mention is a class the team can't find.
    [Test]
    public void TheReadme_MentionsEveryClass()
    {
        string readme = Readme;
        string[] missing = Scripts.Select(file => file.Name).Where(name => !Regex.IsMatch(readme, @"\b" + name + @"\b")).ToArray();
        Assert.IsEmpty(missing, "Not in the README: " + string.Join(", ", missing));
    }

    [Test]
    public void TheReadme_MentionsEveryEventAsset()
    {
        string readme = Readme;
        foreach (string path in Directory.GetFiles(Path.Combine(Application.dataPath, "Data", "Events"), "*.asset"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            StringAssert.Contains(name, readme, $"The event {name} is not in the README");
        }
    }

    // Code that uses UnityEditor would break the WebGL build, unless it is in the Editor folder.
    [Test]
    public void RuntimeCode_DoesNotUseTheEditor()
    {
        string[] users = Runtime.Where(file => Regex.IsMatch(file.Code, @"\bUnityEditor\b")).Select(file => file.ToString()).ToArray();
        Assert.IsEmpty(users, string.Join("\n", users));
    }

    [Test]
    public void RuntimeAssembly_DoesNotReferenceTheEditor()
    {
        string[] references = Game.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        CollectionAssert.DoesNotContain(references, "UnityEditor");
        CollectionAssert.DoesNotContain(references, "UnityEditor.CoreModule");
    }

    // The old Input Manager is off in this project. Reading it would throw in the build.
    [Test]
    public void RuntimeCode_UsesOnlyTheNewInputSystem()
    {
        string[] users = Runtime.Where(file => Regex.IsMatch(file.Code, @"\bInput\.(GetKey|GetAxis|GetButton|GetMouse|mousePosition|touches)")).Select(f => f.ToString()).ToArray();
        Assert.IsEmpty(users, string.Join("\n", users));
    }

    // The course checker runs dotnet-format on the scripts. These are the rules it is strictest about.
    [Test]
    public void EveryFile_FollowsTheWhitespaceRules()
    {
        var problems = new List<string>();
        foreach (string path in Directory.GetFiles(Application.dataPath + "/Scripts", "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Application.dataPath + "/Tests", "*.cs", SearchOption.AllDirectories)))
        {
            string text = File.ReadAllText(path);
            string name = path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
            if (text.Contains("\r"))
            {
                problems.Add(name + ": has CRLF line ends");
            }
            if (text.Contains("\t"))
            {
                problems.Add(name + ": has tabs");
            }
            if (!text.EndsWith("\n") || text.EndsWith("\n\n"))
            {
                problems.Add(name + ": must end with exactly one new line");
            }
            if (Regex.IsMatch(text, @"[ \t]+\n"))
            {
                problems.Add(name + ": has spaces at the end of a line");
            }
            if (Regex.IsMatch(text, @"^\s*(if|else|for|foreach|while|switch)\b.*\{\s*$", RegexOptions.Multiline))
            {
                problems.Add(name + ": has a brace at the end of a line (the style is Allman: braces on their own line)");
            }
        }
        Assert.IsEmpty(problems, Fail(problems));
    }
}
