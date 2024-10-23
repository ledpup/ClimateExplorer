namespace ClimateExplorer.Core.Model;

using System.Linq;
using System.Text.Json;

public class Region : GeographicalEntity
{
    public const string Sun = "Sun";
    public const string Earth = "Earth";
    public const string Land = "Land";
    public const string Atmosphere = "Atmosphere";
    public const string Ocean = "Ocean";

    public const string NorthernHemi = "Northern Hemisphere";
    public const string SouthernHemi = "Southern Hemisphere";

    public const string Arctic = "Arctic";
    public const string Antarctic = "Antarctic";
    public const string Greenland = "Greenland";
    public const string R60s60n = "60°S-60°N";
    public const string R60s60nOcean = "60°S-60°N Ocean";

    public const string NorthernHemiOcean = "Northern Hemisphere Ocean";
    public const string SouthernHemiOcean = "Southern Hemisphere Ocean";

    public const string ArcticOcean = "Arctic Ocean";
    public const string AntarcticOcean = "Antarctic Ocean";

    public static readonly List<Region> PhysicalRegions =
        [
            new Region
            {
                Id = RegionId(Sun),
                Name = Sun,
            },
            new Region
            {
                Id = RegionId(Earth),
                Name = Earth,
            },
            new Region
            {
                Id = RegionId(Land),
                Name = Land,
            },
            new Region
            {
                Id = RegionId(Ocean),
                Name = Ocean,
            },
            new Region
            {
                Id = RegionId(Atmosphere),
                Name = Atmosphere,
            },
            new Region
            {
                Id = RegionId(NorthernHemi),
                Name = NorthernHemi,
            },
            new Region
            {
                Id = RegionId(SouthernHemi),
                Name = SouthernHemi,
            },
            new Region
            {
                Id = RegionId(Arctic),
                Name = Arctic,
            },
            new Region
            {
                Id = RegionId(Antarctic),
                Name = Antarctic,
            },
            new Region
            {
                Id = RegionId(Greenland),
                Name = Greenland,
            },
            new Region
            {
                Id = RegionId(R60s60n),
                Name = R60s60n,
            },
            new Region
            {
                Id = RegionId(R60s60nOcean),
                Name = R60s60nOcean,
            },
            new Region
            {
                Id = RegionId(ArcticOcean),
                Name = ArcticOcean,
            },
            new Region
            {
                Id = RegionId(AntarcticOcean),
                Name = AntarcticOcean,
            },
            new Region
            {
                Id = RegionId(NorthernHemiOcean),
                Name = NorthernHemiOcean,
            },
            new Region
            {
                Id = RegionId(SouthernHemiOcean),
                Name = SouthernHemiOcean,
            },
        ];

    public static readonly Dictionary<string, Region> PhysicalRegionsDict = PhysicalRegions.Select(x => new KeyValuePair<string, Region>(x.Name, x)).ToDictionary();

    public string? CountryCode { get; set; }

    public static Guid RegionId(string name)
    {
        return name switch
        {
            Sun => new Guid("6F4C185E-F846-4DFE-BE28-8DB99A53CF7D"),
            Earth => new Guid("379ACE49-0B44-4B47-AB61-3B6E09C27C82"),
            Land => new Guid("6FA62EA0-F9EC-46CB-A9E5-F610EB6BAC5E"),
            Ocean => new Guid("9107612D-057D-4982-BF93-1F32A01D4EE0"),
            Atmosphere => new Guid("8B00649C-E232-49B3-9065-3866FD1B9952"),
            NorthernHemi => new Guid("E29D2979-B243-49E1-A339-280F588AE878"),
            SouthernHemi => new Guid("1D9FAF5C-77EE-4B3F-B9D2-10D93A176F74"),
            Arctic => new Guid("A2F94904-5EAE-45E5-AFFA-F6F190256C5D"),
            Antarctic => new Guid("C596129B-724C-472A-86BE-8B98901377D4"),
            Greenland => new Guid("F414D04E-E08D-4908-A5A4-F9513D4A9C25"),
            R60s60n => new Guid("DC61D863-32BF-4A4E-AE86-7A34911B8509"),
            R60s60nOcean => new Guid("3AB58FCA-497F-4D21-B93A-76D69E283072"),
            NorthernHemiOcean => new Guid("0504C1B9-5A1A-4157-AF2E-E75EAEB42189"),
            SouthernHemiOcean => new Guid("330FFE23-9D6F-4DE8-B96D-2960CD2EF063"),
            ArcticOcean => new Guid("50523F70-6A9A-4B7C-A824-434CD91328DD"),
            AntarcticOcean => new Guid("4A2EBA37-E623-47D8-9FB9-58F695CEDF72"),
            _ => throw new NotImplementedException()
        };
    }

    public static async Task<List<Region>> GetRegions()
    {
        var folderName = @"MetaData\Region";
        var regionsFromFolder = new List<Region>();
        var regionFiles = Directory.GetFiles(folderName).ToList();
        foreach (var file in regionFiles)
        {
            var locationsInFile = await GetRegionsFromFile(file);
            regionsFromFolder.AddRange(locationsInFile);
        }

        regionsFromFolder = regionsFromFolder.OrderBy(x => x.Name).ToList();

        List<Region> allRegions = [];
        allRegions.AddRange(PhysicalRegions);
        allRegions.AddRange(regionsFromFolder);

        return allRegions;
    }

    public static async Task<List<Region>> GetRegionsFromFile(string pathAndFileName)
    {
        var text = await File.ReadAllTextAsync(pathAndFileName);
        var regions = JsonSerializer.Deserialize<List<Region>>(text);
        return regions!;
    }
}
