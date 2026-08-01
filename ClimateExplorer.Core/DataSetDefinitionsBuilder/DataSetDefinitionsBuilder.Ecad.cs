namespace ClimateExplorer.Core;

using ClimateExplorer.Core.Model;
using static ClimateExplorer.Core.Enums;

public partial class DataSetDefinitionsBuilder
{
    public static readonly Guid EcadDataSetDefinitionId = Guid.Parse("265289F3-D375-437C-A642-A5EC49C8B5F7");

    /// <summary>
    /// ECA&amp;D's four measurements are declared as one dataset rather than split by data type, so a
    /// location either gets all of them from ECA&amp;D or none of them. Splitting would make cross-type
    /// consistency an accident of declaration order, which is the problem the old split GHCNd/GHCNdp
    /// definitions still have.
    /// <para>
    /// Only the non-blended edition exists today, which is why every temperature measurement is
    /// <see cref="DataAdjustment.Unadjusted"/> - the same adjustment GHCNd's daily temperatures carry, so
    /// ECA&amp;D preempts GHCNd for them by declaration order alone. Precipitation is <c>null</c>, as
    /// precipitation always is here; there is no blended precipitation tier to design around, because
    /// only temperature ever splits by blended/non-blended.
    /// </para>
    /// </summary>
    private static List<DataSetDefinition> BuildEcadDataSetDefinitions()
    {
        return
        [
            new()
            {
                Id = EcadDataSetDefinitionId,
                Name = "European Climate Assessment & Dataset (ECA&D)",
                ShortName = "ECA&D",
                Description = "The European Climate Assessment & Dataset (ECA&D) presents daily temperature and precipitation observations from stations across Europe and the Mediterranean, contributed by national meteorological services and observatories. Some series begin in the 18th century. ClimateExplorer uses the non-blended edition, which is the data as submitted by each participant rather than the gap-filled, homogenised blended edition. ECA&D publishes updates for participating European stations more frequently than GHCNd does, so it is preferred over GHCNd wherever a location's station is registered with it.",
                Publisher = "Royal Netherlands Meteorological Institute (KNMI)",
                PublisherUrl = "https://www.knmi.nl/",
                PublisherDivision = "European Climate Assessment & Dataset",
                MoreInformationUrl = "https://www.ecad.eu/dailydata/index.php",
                DataDownloaderKey = "ecad-station",

                // Keyed by GHCN station id, like every other GHCN-family dataset here: the mapping, the
                // source file name and this lookup all use it, and ECA&D's own station id lives only in
                // the crosswalk the downloader consults.
                StationMetadataFileName = "Stations_ghcnm_adjusted.json",
                MeasurementDefinitions =
                [
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMean,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),(?<value>-?\d*\.?\d*),-?\d*\.?\d*,-?\d*\.?\d*,-?\d*\.?\d*$",
                        DataFileSource = ArchiveSource(@"Ecad\Unadjusted\[station].zip", "[station].csv"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMax,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),-?\d*\.?\d*,(?<value>-?\d*\.?\d*),-?\d*\.?\d*,-?\d*\.?\d*$",
                        DataFileSource = ArchiveSource(@"Ecad\Unadjusted\[station].zip", "[station].csv"),
                    },
                    new()
                    {
                        DataAdjustment = DataAdjustment.Unadjusted,
                        DataType = DataType.TempMin,
                        UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),-?\d*\.?\d*,-?\d*\.?\d*,(?<value>-?\d*\.?\d*),-?\d*\.?\d*$",
                        DataFileSource = ArchiveSource(@"Ecad\Unadjusted\[station].zip", "[station].csv"),
                    },
                    new()
                    {
                        DataAdjustment = null,
                        DataType = DataType.Precipitation,
                        UnitOfMeasure = UnitOfMeasure.Millimetres,
                        DataResolution = DataResolution.Daily,
                        RowDataType = RowDataType.OneValuePerRow,
                        DataRowRegEx = @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2}),-?\d*\.?\d*,-?\d*\.?\d*,-?\d*\.?\d*,(?<value>-?\d*\.?\d*)$",
                        DataFileSource = ArchiveSource(@"Ecad\Unadjusted\[station].zip", "[station].csv"),
                    },
                ],
            },
        ];
    }
}
