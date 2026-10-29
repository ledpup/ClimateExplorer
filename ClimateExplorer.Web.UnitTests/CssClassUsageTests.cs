namespace ClimateExplorer.Web.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Keeps CSS class selectors in step with the markup. A class name is a plain string in markup, C# and CSS, so a
/// rename that misses one side, or a rule left behind when its markup is deleted, still compiles and quietly
/// stops styling anything.
/// </summary>
[TestClass]
public partial class CssClassUsageTests
{
    private static readonly string[] Projects = ["ClimateExplorer.Web.Client", "ClimateExplorer.Web"];

    private static readonly string[] SourceExtensions = [".razor", ".cs", ".js", ".html"];

    // Classes that no source file names in full, and why. An entry ending in '-' covers every class with that
    // prefix. Each must still match a selector, so the list can only shrink.
    private static readonly Dictionary<string, string> AllowList = new(StringComparer.Ordinal)
    {
        ["notification-type-"] = "NotificationHost.razor.cs builds these by interpolation from the NotificationType.",
        ["status-"] = "The recent observation tiles write status-@RecentObservationTileDisplay.StatusClass(...).",
        ["large"] = "Collapsible.razor writes CollapserSize lower-cased.",
        ["extralarge"] = "Collapsible.razor writes CollapserSize lower-cased.",
    };

    [TestMethod]
    public void CssClassSelectors_AllStylesheets_EachUsedInSource()
    {
        var sourceTokens = Tokens(string.Join(Environment.NewLine, FindSourceFiles().Select(File.ReadAllText)));

        var unused = FindStylesheets()
            .SelectMany(x => x.Selectors.SelectMany(ClassesIn).Select(c => (x.File, Class: c)))
            .Where(x => !sourceTokens.Contains(x.Class) && !IsAllowListed(x.Class))
            .Select(x => $"{x.File}: .{x.Class}")
            .Distinct()
            .ToList();

        Assert.IsEmpty(unused, "No markup or code uses these classes. Delete the rule, or fix the markup that should use it:" + Environment.NewLine + string.Join(Environment.NewLine, unused));
    }

    [TestMethod]
    public void CssClassSelectors_AllowList_EveryEntryStillNeeded()
    {
        var classes = FindStylesheets().SelectMany(x => x.Selectors.SelectMany(ClassesIn)).ToHashSet(StringComparer.Ordinal);

        var unneeded = AllowList.Keys.Where(x => !classes.Any(c => Matches(x, c))).ToList();

        Assert.IsEmpty(unneeded, "Remove from the allow-list: " + string.Join(", ", unneeded));
    }

    /// <summary>
    /// Blazor puts a component's scope attribute only on the elements in its own markup, so a scoped rule can't
    /// reach an element a child component renders - including one that got the class through the child's
    /// <c>Class</c> parameter - unless the rule goes through <c>::deep</c>.
    /// </summary>
    [TestMethod]
    public void ScopedCssClassSelectors_BeforeDeep_RenderedByOwnComponent()
    {
        var violations = new List<string>();

        foreach (var sheet in FindStylesheets().Where(x => x.File.EndsWith(".razor.css", StringComparison.OrdinalIgnoreCase)))
        {
            var razorPath = sheet.FullPath[..^".css".Length];

            if (!File.Exists(razorPath))
            {
                continue;
            }

            var ownTokens = Tokens(RemoveComponentClassAttributes(File.ReadAllText(razorPath)) + ReadIfExists(razorPath + ".cs") + ReadIfExists(razorPath + ".js"));

            violations.AddRange(
                sheet.Selectors
                    .SelectMany(x => ClassesIn(x.Split("::deep")[0]))
                    .Distinct()
                    .Where(x => !ownTokens.Contains(x) && !IsAllowListed(x))
                    .Select(x => $"{sheet.File}: .{x}"));
        }

        Assert.IsEmpty(violations, "The component doesn't render these classes itself. Reach a child component's markup with ::deep:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    // Quoted strings and attribute selectors, which can contain dots that aren't classes ([href$=".pdf"]).
    [GeneratedRegex("""\[[^\]]*\]|"[^"]*"|'[^']*'""")]
    private static partial Regex NonClassTextPattern();

    [GeneratedRegex(@"\.(-?[A-Za-z_][\w-]*)")]
    private static partial Regex ClassPattern();

    [GeneratedRegex(@"[A-Za-z_][\w-]*")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"\sClass=""")]
    private static partial Regex ComponentClassAttributePattern();

    private static IEnumerable<(string File, string FullPath, List<string> Selectors)> FindStylesheets()
    {
        return FindProjectFiles([".css"]).Select(x => (x.Relative, x.FullPath, ParseSelectors(File.ReadAllText(x.FullPath))));
    }

    private static IEnumerable<string> FindSourceFiles()
    {
        return FindProjectFiles(SourceExtensions).Select(x => x.FullPath);
    }

    private static IEnumerable<(string Relative, string FullPath)> FindProjectFiles(string[] extensions)
    {
        foreach (var project in Projects)
        {
            var root = SourceTree.FindProjectRoot(project);

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

                // Build output, and vendored libraries (Font Awesome, Chart.js) whose CSS styles their own markup.
                if (relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("wwwroot/lib/", StringComparison.OrdinalIgnoreCase)
                    || !extensions.Any(x => relative.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                yield return ($"{project}/{relative}", file);
            }
        }
    }

    // Each '{' ends a prelude: a selector list, or an at-rule's (@media, @keyframes), which is skipped. Each ';' or
    // '}' ends a declaration, which is discarded. That way selectors nested in @media are kept.
    private static List<string> ParseSelectors(string css)
    {
        var selectors = new List<string>();
        var buffer = new StringBuilder();
        var quote = '\0';

        foreach (var ch in CommentPattern().Replace(css, " "))
        {
            if (quote != '\0')
            {
                buffer.Append(ch);
                quote = ch == quote ? '\0' : quote;
                continue;
            }

            switch (ch)
            {
                case '"' or '\'':
                    quote = ch;
                    buffer.Append(ch);
                    break;
                case '{':
                    var prelude = buffer.ToString().Trim();

                    if (prelude.Length > 0 && prelude[0] != '@')
                    {
                        selectors.AddRange(prelude.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0));
                    }

                    buffer.Clear();
                    break;
                case ';' or '}':
                    buffer.Clear();
                    break;
                default:
                    buffer.Append(ch);
                    break;
            }
        }

        return selectors;
    }

    private static IEnumerable<string> ClassesIn(string selector)
    {
        return ClassPattern().Matches(NonClassTextPattern().Replace(selector, " ")).Select(x => x.Groups[1].Value);
    }

    private static HashSet<string> Tokens(string text)
    {
        return TokenPattern().Matches(text).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
    }

    // Drops the value of each component's Class="..." parameter, which the child component renders. A Razor
    // expression in the value can contain quotes (Class="@(x ? "a" : "b")"), so quotes only end it outside parentheses.
    private static string RemoveComponentClassAttributes(string razor)
    {
        var result = new StringBuilder();
        var position = 0;

        for (var match = ComponentClassAttributePattern().Match(razor); match.Success; match = ComponentClassAttributePattern().Match(razor, position))
        {
            var valueStart = match.Index + match.Length;
            var depth = 0;
            var end = valueStart;

            while (end < razor.Length && (razor[end] != '"' || depth > 0))
            {
                depth += razor[end] switch { '(' => 1, ')' => -1, _ => 0 };
                end++;
            }

            result.Append(razor, position, valueStart - position);
            position = end;
        }

        return result.Append(razor, position, razor.Length - position).ToString();
    }

    private static string ReadIfExists(string path)
    {
        return File.Exists(path) ? Environment.NewLine + File.ReadAllText(path) : string.Empty;
    }

    private static bool IsAllowListed(string className)
    {
        return AllowList.Keys.Any(x => Matches(x, className));
    }

    private static bool Matches(string allowListEntry, string className)
    {
        return allowListEntry.EndsWith('-')
            ? className.StartsWith(allowListEntry, StringComparison.Ordinal)
            : className == allowListEntry;
    }
}
