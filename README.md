# Climate Explorer

[Climate Explorer](https://www.climateexplorer.net/) is a website to help people understand climate change. It's focussed on trying to provide a simple and approachable interface for people to explore. It's available for Australian and New Zealand locations. The data is sourced from:

- [BOM](http://www.bom.gov.au/)
- [NIWA](https://niwa.co.nz/)
- [NOAA](https://www.noaa.gov/)

## Glossary
- [ACORN-SAT](http://www.bom.gov.au/climate/data/acorn-sat/): Australian Climate Observations Reference Network – Surface Air Temperature
- [RAIA](http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks): Remote Australian Islands and Antarctica (a monthly dataset that is a smaller part of ACORN-SAT)
- [NIWA](https://niwa.co.nz/): New Zealand's National Institute of Water and Atmospheric Research (maintains the [11-station series](https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data))
- [NOAA](https://www.noaa.gov/): National Oceanic and Atmospheric Administration

## Technical
- Built in [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/)
- C#, .NET 6
- Blazor, using
  - https://github.com/Megabit/Blazorise
  - https://github.com/DP-projects/DPBlazorMap
  - https://github.com/AeonLucid/GeoCoordinate.NetStandard1
- There are five projects:
  - Visualiser: Blazor website that displays the data to the user
  - WebApi: Web API that gets and processes the data that Visualiser uses
  - Core: shared files between Visualiser, Analyser and WebApi
  - UnitTests: tests for various sub-systems
  - Analyser: console app for manipulating datasets... not used much

## How to use

- Download the github repo. 
- Open in Visual Studio 2022. 
- Set your start-up projects to be Visualiser and WebApi and run. Two websites should start-up; the user interface and the web API.

## TODO
In rough order of priority

- Display info on missing data (number of records missing per week/month/etc for min and max, number of missing consecutive days, etc.)
- Analyse missing data to find an optimal grouping method - linear regression analysis??
- Write an adaptor to query NIWA CliFlo website to pull in the 11-station series data automatically
- Get ENSO data from URLs for indexes (still supporting offline mode)
- Alternate averages (use median/mode - not sure how useful that is)

### Done

- 2022-06-22: Major graphical and functional redesign
- 2022-06-22: Show graphs from two or more locations at once
- 2022-05-19: Display the heating score as the map markers for each location on the map
- 2022-05-19: Changed to use DPBlazorMap as the leaflet Blazor component
- 2022-05-15: GHGs (CO2, CH4, etc) data and graphs
- 2022-04-25: Naviagation history in the browser (can go backward/forward) + supports bookmarking
- 2022-04-25: Climate stripes. https://www.reading.ac.uk/en/planet/climate-resources/climate-stripes
- 2022-04-23: Major re-work the on the styling/layout
- 2022-04-23: Switch to Blazorise layout elements and NavMenu
- 2022-04-18: Linear trendline
- 2022-01-31: Support graphs for rainfall
- 2022-01-29: Small map initially. Click to expand to select location. Collapse map after selection.
- 2022-01-27: Download datasets as CSV - yearly data only, for now
- 2022-01-26: Alter web API to look for precalculated data in a cache instead of calculating from daily. If no cached data, calculate and save to cache
- 2022-01-19: Get temperature data from BOM via website (http://www.bom.gov.au/climate/data/index.shtml?bookmark=122&view=map)
- 2022-01-16: Show as a bar chart, the difference between adjusted and unadjusted
- 2022-01-16: Show bearing to each suggested nearby station
- 2022-01-16: Include New Zealand's NIWA 'seven-station' temperature series https://niwa.co.nz/climate/information-and-resources/nz-temperature-record
- 2022-01-15: Display nearby locations, with km distance and hyperlink. Note: still needs some improvement
- 2022-01-14: Allow user to alter the parameters that calculate the set of data for the year (dayGrouping and threshold)
- 2022-01-13: Put the selected year, start year, end year into a modal filter section.
- 2022-01-13: Bar chart of temperatures relative to average - https://www.reading.ac.uk/en/planet/climate-resources/climate-stripes
- 2022-01-08: Use current location to selected location
- 2022-01-07: Add graphs for Remote Australian Islands and Antarctica (RAIA)
- 2022-01-03: Aggregates; latitude bands (-10 to -20, -20 to -30, -30 to -40, -40 to -50)
- 2022-01-03: Aggregates; Australia
- 2022-01-01: Weekly data resolution when looking at charts for a year (instead of daily)
- 2022-01-01: Calculate averages on demand rather than precalculating
- 2021-12-31: Averages of averages; average by month or any arbitrary grouping of days
- 2021-12-31: Better chart colours (from https://observablehq.com/@d3/color-schemes)
- 2021-12-26: Click point on yearly chart to render daily chart for the year
- 2021-12-26: Retrieve ENSO data to show its effect on temperature in Australia. https://psl.noaa.gov/enso/dashboard.html, https://psl.noaa.gov/enso/data.html
- 2021-12-21: Simple Moving Average
- 2021-12-12: Integrate map component to show location (from site lat/long)
- 2021-12-12: Create a location JSON file, with unique ID for each location and a site list of BOM sites. (BOM change their site IDs between versions of ACORN-SAT! Need an independant, unique ID.) Include geo-spatial data.
- 2021-12-11: Add start and end year fields for the graph (so the user can constrain the series to the years they are interested in)
- 2021-12-11: Ensure the temperature data is sent to the chart component on the correct starting year (n.b., datasets have different starting years)
