# AcornSatAnalyser
Tool to calculate average temperatures from BOM ACORN-SAT data.

## Technical
- Built in Visual Studio 2022 Community Edition
- C#, .NET 6
- Blazor (using Blazorise components)
- There are 4 projects:
  - Analyser: take daily data (raw and adjusted) and average it into
  - WebApi: gets the data for the website to use
  - Visualiser: displays the data to the user. Primarily it's a line graph
  - Core: some shared files, between Analyser and WebApi
  - UnitTests: tests for various sub-systems

## TODO
- Better moving average
- CO2 data and graphs
- Climate stripes. https://www.reading.ac.uk/en/planet/climate-resources/climate-stripes
- Linear trendline
- Alternate averages (use median/mode - not sure how useful that is)
- Show graphs from two or more locations at once? (not sure how useful that is)
- Allow user to alter thresold for what is a sufficient set of data for the year
- Averages of averages; average by month or 5-season groupings
- Averages of averages; latitude bands
- Averages of averages; Australia
- Different units of measure (Fahrenheit + Kelvin)

### Done
- 2022-01-01: Weekly data resolution when looking at charts for a year (instead of daily)
- 2022-01-01: Calculate averages on demand rather than precalculating
- 2021-12-31: Better chart colours (from https://observablehq.com/@d3/color-schemes)
- 2021-12-26: Click point on yearly chart to render daily chart for the year
- 2021-12-26: Retrieve ENSO data to show its effect on temperature in Australia. https://psl.noaa.gov/enso/dashboard.html, https://psl.noaa.gov/enso/data.html
- 2021-12-21: Simple Moving Average
- 2021-12-12: Integrate map component to show location (from site lat/long)
- 2021-12-12: Create a location JSON file, with unique ID for each location and a site list of BOM sites. (BOM change their site IDs between versions of ACORN-SAT! Need an independant, unique ID.) Include geo-spatial data.
- 2021-12-11: Add start and end year fields for the graph (so the user can constrain the series to the years they are interested in)
- 2021-12-11: Ensure the temperature data is sent to the chart component on the correct starting year (n.b., datasets have different starting years)

## Data sources

### BOM ACORN-SAT temperature

Files sourced from:
- ftp://ftp.bom.gov.au/anon/home/ncc/www/change/ACORN_SAT_daily/
- Accessed 2021-12-09

Original adjusted file names are:
- acorn_sat_v2.2.0_daily_tmax.tar.gz
- acorn_sat_v2.2.0_daily_tmin.tar.gz

Raw data file is:
- raw-data-and-supporting-information.zip

#### Adjustments

The tmax and tmin file lengths aren't all the same. Some start on different dates to others, when there are missing records at the start or the end. It's usually just a few days. For the locations below, I adjusted the files so they all start and end on the same day, by adding blank rows. I explain in detailed for Esperance. All the other changes are similar adjustments. No other changes have been made to the v2.2.0 homogenised data from the BOM.

- Esperance, 009789; tmax daily file starts on 1910-01-03. tmin starts on 1910-01-02. Added a null 1910-01-02 row for tmax
- Forrest, 011052
- Gabo Island, 084016
- Low Head, 091293
- Normanton, 029063
- Oodnadatta, 017043
- Orbost, 084145
- Rockhampton, 039083

### CO2

NOAA
- ftp://aftp.cmdl.noaa.gov/products/trends/co2/co2_annmean_mlo.txt
- Accessed 2021-12-21
- https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_mm_mlo.txt
- Accessed 2021-12-27

### ENSO 

#### MEI v2

The MEI combines both oceanic and atmospheric variables to form a single index an assessment of ENSO. It is an Empirical Orthogonal Function (EOF) of five different variables (sea level pressure (SLP), sea surface temperature (SST), zonal and meridional components of the surface wind, and outgoing longwave radiation (OLR)) over the tropical Pacific basin (30°S-30°N and 100°E-70°W).

https://psl.noaa.gov/enso/mei/

- File name: meiv2.data 
- https://psl.noaa.gov/enso/mei/data/meiv2.data
- Accessed 2021-12-21

#### Other ENSO indexes

ONI
- File name: oni.data.txt
- https://psl.noaa.gov/data/correlation/oni.data
- Accessed 2021-12-25

OSI 
- File name: soi.long.data.txt
- https://psl.noaa.gov/gcos_wgsp/Timeseries/Data/soi.long.data
- Accessed 2021-12-25