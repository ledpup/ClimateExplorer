using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

const string ReferenceFolder = "Reference";
const string StationsCsv = "acorn_sat_stations.csv";
const string StationsTxt = "acorn_sat_stations.txt";
const string FailedCodesFile = "failed_product_codes.txt";
const string MappingFile = "acorn_station_product_mapping.csv";
const string ValidResponsesFile = "valid_responses.csv";

BomProductMapper.RunAsync(args).GetAwaiter().GetResult();


