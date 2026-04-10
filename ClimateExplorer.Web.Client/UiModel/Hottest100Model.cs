namespace ClimateExplorer.Web.Client.UiModel
{
    using ClimateExplorer.Core.Model;

    public static class Hottest100Model
    {
        public const int ChartWidth = 900;
        public const int ChartHeight = 200;

        public static List<RecordCount> BuildYearCounts(IEnumerable<ClimateRecord> records)
        {
            return [.. records
                .GroupBy(r => r.Year)
                .Select(g => new RecordCount { Year = g.Key, Count = g.Count() })];
        }

        public static string Generate(List<RecordCount> yearCounts, int startYear, int endYear)
        {
            const int height = 150;
            const int paddingLeft = 12;
            const int paddingRight = 12;
            const int paddingTop = 20;
            const int paddingBottom = 20;
            const int chartWidth = ChartWidth - paddingLeft - paddingRight;
            const int chartHeight = height - paddingTop - paddingBottom;

            int minYear = startYear;
            var maxYear = endYear;
            var yearRange = maxYear - minYear;

            double ToX(int year) => paddingLeft + ((double)(year - minYear) / yearRange * chartWidth);

            var svg = new System.Text.StringBuilder();

            // X-axis
            var xAxisY = height - paddingBottom;
            svg.AppendLine($@"  <line x1=""{paddingLeft}"" y1=""{xAxisY}"" x2=""{ChartWidth - paddingRight}"" y2=""{xAxisY}"" stroke=""#333"" stroke-width=""1""/>");

            // X-axis labels and tick marks (every 10 years)
            var firstLabelYear = minYear + ((10 - (minYear % 10)) % 10);
            for (int year = firstLabelYear; year <= maxYear; year += 10)
            {
                var x = ToX(year);
                svg.AppendLine($@"  <line x1=""{x:F2}"" y1=""{xAxisY}"" x2=""{x:F2}"" y2=""{xAxisY + 7}"" stroke=""#333"" stroke-width=""0.5""/>");
                svg.AppendLine($@"  <text x=""{x:F2}"" y=""{xAxisY + 22}"" text-anchor=""middle"" font-size=""9"" fill=""#666"">{year}</text>");
            }

            // Vertical bars: all same height, width indicates count of hottest days (clamped to prevent overlap)
            const double barHeight = chartHeight * 0.95;

            // Calculate maximum bar width to prevent overlap between adjacent years
            var pixelsPerYear = (double)chartWidth / yearRange;
            var maxBarWidth = pixelsPerYear * 0.95; // Use 95% of space to leave small gap
            var maxCount = Math.Max(yearCounts.Max(x => x.Count), 10);
            var singleLineThickness = maxBarWidth / maxCount;

            foreach (var yc in yearCounts.OrderBy(t => t.Year))
            {
                var x = ToX(yc.Year);
                var yTop = xAxisY - barHeight;

                // Width proportional to count, but clamped to prevent overlap
                var barWidth = Math.Min(yc.Count * singleLineThickness, maxBarWidth);

                svg.AppendLine($@"  <rect x=""{x - (barWidth / 2):F2}"" y=""{yTop:F2}"" width=""{barWidth:F2}"" height=""{barHeight}"" fill=""#000""/>");
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
