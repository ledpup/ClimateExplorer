namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Keeps unit text inside the display layer. Any client code that writes a unit label itself, or calls
/// Core's label and rounding helpers directly, would bypass <c>IUnitFormatter</c> and show metric text
/// to a US customary reader - or, worse, a converted number with a metric label.
/// </summary>
[TestClass]
public partial class UnitTextArchitectureTests
{
    // Files allowed unit text, and why. Each must still contain some, so the list can only shrink.
    private static readonly Dictionary<string, string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pages/About.razor"] = "Explanatory prose about how observations are made.",
        ["Components/Common/Trends/TrendsOverviewExplainer.razor"] = "A worked metric example; converting it would mean rewriting the arithmetic.",
        ["UiModel/SuggestedPresetLists.LocationBased.cs"] = "Preset descriptions name the metric thresholds their calculations use.",
        ["Services/Trends/TrendFormatting.cs"] = "The metric label tables used by the always-metric regression report.",
        ["UiLogic/ChartOptionsFactory.cs"] = "Core's axis title for frost-day and custom series, which count something other than a unit.",
        ["Components/AboutDataDetails.razor.cs"] = "Describes the unit a dataset is published in.",
        ["Components/Common/UnitSwitch.razor"] = "The switch's accessible name says which units it switches to.",
    };

    [TestMethod]
    public void WebClientSources_UnitText_OnlyInUnitsFolderOrAllowList()
    {
        var violations = FindUnitText()
            .Where(x => !AllowList.ContainsKey(x.File))
            .Select(x => $"{x.File}:{x.Line}: {x.Text}")
            .ToList();

        Assert.IsEmpty(violations, "Format through IUnitFormatter instead:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void WebClientSources_AllowList_EveryEntryStillNeeded()
    {
        var filesWithUnitText = FindUnitText().Select(x => x.File).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unneeded = AllowList.Keys.Where(x => !filesWithUnitText.Contains(x)).ToList();

        Assert.IsEmpty(unneeded, "Remove from the allow-list: " + string.Join(", ", unneeded));
    }

    // Unit labels, and the Core helpers that produce them. "mm" only counts as a unit when it follows a
    // number or an interpolation hole, or is a string on its own, so words like "summary" don't match.
    [GeneratedRegex("""°C|°F|km²|mi²|"mm"|[0-9}]mm\b|UnitOfMeasureLabelShort\(|UnitOfMeasureLabel\(|UnitOfMeasureRounding\(""")]
    private static partial Regex UnitTextPattern();

    private static IEnumerable<(string File, int Line, string Text)> FindUnitText()
    {
        var root = FindWebClientRoot();

        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

            if (relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("Units/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Comments may describe units freely.
                if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*') || line.StartsWith("/*", StringComparison.Ordinal) || line.StartsWith("@*", StringComparison.Ordinal))
                {
                    continue;
                }

                if (UnitTextPattern().IsMatch(line))
                {
                    yield return (relative, i + 1, line);
                }
            }
        }
    }

    private static string FindWebClientRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            // The project folder, not a build output folder of the same name.
            var candidate = Path.Combine(directory.FullName, "ClimateExplorer.Web.Client");

            if (File.Exists(Path.Combine(candidate, "ClimateExplorer.Web.Client.csproj")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Couldn't find ClimateExplorer.Web.Client above " + AppContext.BaseDirectory);
    }
}
