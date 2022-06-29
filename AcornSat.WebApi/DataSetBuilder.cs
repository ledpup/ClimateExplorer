using AcornSat.Core.InputOutput;
using AcornSat.WebApi.Model.DataSetBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcornSat.WebApi
{
    public class DataSetBuilder
    {
        public async Task<DataSet> BuildDataSet(PostDataSetsRequestBody request)
        {
            ValidateRequest(request);

            var dataPoints = await GetSeriesDataPointsForRequest(request);

            return
                new DataSet()
                {
                    MeasurementDefinition = new Core.ViewModel.MeasurementDefinitionViewModel(),
                    Location = new Location(),
                    DataRecords = 
                        dataPoints
                        .Select(
                            x => 
                            new DataRecord()
                            {
                                Year = x.Year,
                                Month = x.Month,
                                Day = x.Day,
                                Value = x.Value
                            }
                        )
                        .ToList()
                };
        }

        public void ValidateRequest(PostDataSetsRequestBody request)
        {
            if (request.SeriesSpecifications == null) throw new ArgumentNullException(nameof(request.SeriesSpecifications));
        }

        async Task<DataPoint[]> GetSeriesDataPointsForRequest(PostDataSetsRequestBody request)
        {
            switch (request.SeriesDerivationType)
            {
                case SeriesDerivationTypes.ReturnSingleSeries:
                    if (request.SeriesSpecifications.Length != 1)
                    {
                        throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.ReturnSingleSeries)}, exactly one SeriesSpecification must be provided.");
                    }

                    return await GetSeriesDataPoints(request.SeriesSpecifications.Single());

                case SeriesDerivationTypes.DifferenceBetweenTwoSeries:
                    if (request.SeriesSpecifications.Length != 2)
                    {
                        throw new Exception($"When SeriesDerivationType is {nameof(SeriesDerivationTypes.DifferenceBetweenTwoSeries)}, exactly two SeriesSpecifications must be provided.");
                    }

                    var dp1 = await GetSeriesDataPoints(request.SeriesSpecifications[0]);
                    var dp2 = await GetSeriesDataPoints(request.SeriesSpecifications[1]);

                    if (dp1.Length == 0 || dp2.Length == 0)
                    {
                        return new DataPoint[0];
                    }

                    var dp1Grouped = dp1.ToDictionary(x => new DateOnly(x.Year, x.Month ?? 1, x.Day ?? 1));
                    var dp2Grouped = dp2.ToDictionary(x => new DateOnly(x.Year, x.Month ?? 1, x.Day ?? 1));

                    DateOnly minDate1 = dp1Grouped.Select(x => x.Key).Min();
                    DateOnly minDate2 = dp2Grouped.Select(x => x.Key).Min();

                    DateOnly maxDate1 = dp1Grouped.Select(x => x.Key).Max();
                    DateOnly maxDate2 = dp2Grouped.Select(x => x.Key).Max();

                    DateOnly minDate = minDate1 < minDate2 ? minDate1 : minDate2;
                    DateOnly maxDate = maxDate1 > maxDate2 ? maxDate1 : maxDate2;

                    DateOnly d = minDate;

                    List<DataPoint> results = new List<DataPoint>();

                    while (d <= maxDate)
                    {
                        dp1Grouped.TryGetValue(d, out var dp1ForDate);
                        dp2Grouped.TryGetValue(d, out var dp2ForDate);

                        float? val = null;

                        if (dp1ForDate != null && dp2ForDate != null)
                        {
                            val = dp1ForDate.Value - dp2ForDate.Value;
                        }

                        var dpToUse = dp1ForDate ?? dp2ForDate;

                        results.Add(new DataPoint() { Year = dpToUse.Year, Month = dpToUse.Month, Day = dpToUse.Day, Value = val });

                        d = d.AddDays(1);
                    }

                    return results.ToArray();

                default:
                    throw new NotImplementedException($"SeriesDerivationType {request.SeriesDerivationType}");

            }
        }

        async Task<DataPoint[]> GetSeriesDataPoints(SeriesSpecification seriesSpecification)
        {
            var definitions = await DataSetDefinition.GetDataSetDefinitions();

            var dsd = definitions.Single(x => x.Id == seriesSpecification.DataSetDefinitionId);

            var measurementDefinition =
                dsd.MeasurementDefinitions
                .Single(
                    x =>
                    x.DataType == seriesSpecification.DataType &&
                    x.DataAdjustment == seriesSpecification.DataAdjustment);

            var location = seriesSpecification.LocationId == null ? null : (await Location.GetLocations(dsd.FolderName)).Single(x => x.Id == seriesSpecification.LocationId);

            var dataSets = await DataReader.ReadDataFile(dsd.FolderName, measurementDefinition, dsd.DataResolution, location);

            dataSets = dataSets.Where(x => x.DataRecords != null).ToList();

            if (dataSets.Count > 1)
            {
                // TODO: Include SeriesSpecification in thrown exception
                throw new Exception("Multiple data sets returned.");
            }

            if (dataSets.Count == 0)
            {
                // TODO: Include SeriesSpecification in thrown exception
                throw new Exception("No data sets available.");
            }

            return dataSets.Single().DataRecords.Select(x => new DataPoint { Year = x.Year, Month = x.Month, Day = x.Day, Value = x.Value }).ToArray();
        }
    }
}
