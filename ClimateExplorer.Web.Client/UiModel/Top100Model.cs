﻿namespace ClimateExplorer.Web.Client.UiModel
{
    using System.Globalization;
    using ClimateExplorer.Core.Model;

    /// <summary>
    /// Draws the Top 100 chart at the real pixel width of its container, rather than at a fixed size
    /// scaled to fit with a viewBox, which would scale the text too. Text sizes and colours match
    /// Chart.js's defaults so the chart sits alongside the Chart.js charts.
    /// </summary>
    public static class Top100Model
    {
        public const string FontFamily = "'Helvetica Neue', Helvetica, Arial, sans-serif";
        public const int FontSize = 12;

        // Baselines matching where Chart.js draws its title and subtitle (with the subtitle's 15px bottom padding).
        public const int TitleY = 22;
        public const int SubtitleY = 46;

        public const int ChartHeight = 206;

        private const int PlotTop = 64;
        private const int PlotHeight = 110;
        private const int PaddingX = 16;
        private const int TickLength = 8;
        private const int MinimumLabelSpacing = 40;
        private static readonly int[] LabelSteps = [10, 20, 25, 50, 100];

        public static List<RecordCount> BuildYearCounts(IEnumerable<DataRecord> records)
        {
            return [.. records
                .GroupBy(r => r.Year)
                .Select(g => new RecordCount { Year = g.Key, Count = g.Count() })];
        }

        public static string Generate(List<RecordCount> yearCounts, int startYear, int endYear, int width)
        {
            var plotWidth = width - (PaddingX * 2);
            var yearRange = Math.Max(endYear - startYear, 1);
            var pixelsPerYear = (double)plotWidth / yearRange;

            double ToX(int year) => PaddingX + ((year - startYear) * pixelsPerYear);

            var svg = new System.Text.StringBuilder();
            var culture = CultureInfo.InvariantCulture;

            // X-axis
            const int xAxisY = PlotTop + PlotHeight;
            svg.AppendLine(culture, $@"  <line x1=""{PaddingX}"" y1=""{xAxisY}"" x2=""{width - PaddingX}"" y2=""{xAxisY}"" stroke=""#333"" stroke-width=""1""/>");

            // X-axis labels and tick marks, as far apart as the width needs to keep the labels from crowding
            var labelStep = LabelSteps.FirstOrDefault(step => step * pixelsPerYear >= MinimumLabelSpacing, LabelSteps[^1]);
            var firstLabelYear = startYear + ((labelStep - (startYear % labelStep)) % labelStep);
            for (int year = firstLabelYear; year <= endYear; year += labelStep)
            {
                var x = ToX(year);
                svg.AppendLine(culture, $@"  <line x1=""{x:F2}"" y1=""{xAxisY}"" x2=""{x:F2}"" y2=""{xAxisY + TickLength}"" stroke=""#333"" stroke-width=""0.5""/>");
                svg.AppendLine(culture, $@"  <text x=""{x:F2}"" y=""{xAxisY + TickLength + 14}"" text-anchor=""middle"" font-size=""{FontSize}"" fill=""#666"">{year}</text>");
            }

            // Vertical bars: all same height, width indicates count of hottest days (clamped to prevent overlap)
            const double barHeight = PlotHeight * 0.95;
            const double yTop = xAxisY - barHeight;

            // Calculate maximum bar width to prevent overlap between adjacent years
            var maxBarWidth = pixelsPerYear * 0.95; // Use 95% of space to leave small gap
            var maxCount = Math.Max(yearCounts.Max(x => x.Count), 10);
            var singleLineThickness = maxBarWidth / maxCount;

            foreach (var yc in yearCounts.OrderBy(t => t.Year))
            {
                var x = ToX(yc.Year);

                // Width proportional to count, but clamped to prevent overlap. At least a pixel, so a single record still shows.
                var barWidth = Math.Max(Math.Min(yc.Count * singleLineThickness, maxBarWidth), 1);

                svg.AppendLine(culture, $@"  <rect x=""{x - (barWidth / 2):F2}"" y=""{yTop:F2}"" width=""{barWidth:F2}"" height=""{barHeight:F2}"" fill=""#000""/>");
            }

            // Invisible per-year hover targets for the tooltip. These tessellate exactly across the
            // full plot width (one year's-worth of pixels each) so even a single-record year, whose
            // visible bar may be only a pixel or two wide, is easy to hover.
            foreach (var yc in yearCounts.OrderBy(t => t.Year))
            {
                var x = ToX(yc.Year);
                var hitLeft = Math.Max(PaddingX, x - (pixelsPerYear / 2));
                var hitRight = Math.Min(width - PaddingX, x + (pixelsPerYear / 2));

                svg.AppendLine(culture, $@"  <rect x=""{hitLeft:F2}"" y=""{PlotTop}"" width=""{hitRight - hitLeft:F2}"" height=""{PlotHeight}"" fill=""transparent"" data-year=""{yc.Year}""/>");
            }

            return svg.ToString();
        }

        public record RecordCount
        {
            public int Year { get; set; }
            public int Count { get; set; }
        }
    }
}
