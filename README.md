# AcornSatAnalyser
Tool to calculate average temperatures from BOM ACORN-SAT data

TODO:
- Integrate map component to show location (from site lat/long)
- Trendline
- Alternate average (use median?)
- Show graphs from two or more locations at once? (not sure how useful that is)
- Averages of averages; latitude bands
- Averages of averages; Australia

Done:
- Add start and end year fields for the graph (so the user can constrain the series to the years they are interested in)
- Ensure the temperature data is sent to the chart component on the correct starting year (n.b., datasets have different starting years)

BOM ACORN-SAT data sources

Original file names are:
- acorn_sat_v2.2.0_daily_tmax.tar.gz
- acorn_sat_v2.2.0_daily_tmin.tar.gz

Raw data file is:
- raw-data-and-supporting-information.zip

Files sourced from:
- ftp://ftp.bom.gov.au/anon/home/ncc/www/change/ACORN_SAT_daily/
- Accessed 09/12/2021

Adjustments:

The tmax and tmin file lengths aren't all the same. Some start on different dates to others, when there are missing records at the start or the end. It's usually just a few days. For the locations below, I adjusted the files so they all start and end on the same day, by adding blank rows. I explain in detailed for Esperance. All the other changes are similar adjustments. No other changes have been made to the v2.2.0 homogenised data from the BOM.

- Esperance,009789; tmax daily file starts on 1910-01-03. tmin starts on 1910-01-02. Added a null 1910-01-02 row for tmax
- Forrest,011052
- Gabo Island,084016
- Low Head,091293
- Normanton,029063
- Oodnadatta,017043
- Orbost,084145
- Rockhampton,
