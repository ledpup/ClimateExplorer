# Climate Explorer

[Climate Explorer](https://www.climateexplorer.net/) is a website to help people understand climate change. It's focussed on trying to provide a simple and approachable interface for people to explore the changes to climate in their region. This github site is the digital repository for everything used to bring [the website](https://www.climateexplorer.net/) together.

The data is sourced from:

- [NOAA](https://www.noaa.gov/) United States of America's **National Oceanic and Atmospheric Administration**, a scientific and regulatory agency within the United States Department of Commerce
- [NCEI](https://www.ncei.noaa.gov/) United States of America's **National Centers for Environmental Information** (NCEI), is a U.S. government agency that manages one of the world's largest archives of atmospheric, coastal, geophysical, and oceanic data. It is an office of NOAA, which operates under the U.S. Department of Commerce
- [BOM](http://www.bom.gov.au/) Australia's **Bureau of Meteorology**
- [NIWA](https://niwa.co.nz/) New Zealand's **National Institute of Water and Atmospheric Research** (maintains the [7-stations](https://niwa.co.nz/seven-stations) and [11-station](https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data) series)
- [NSIDC](https://nsidc.org/home) United States of America's **National Snow & Ice Data Center**
- [Met Office](https://www.metoffice.gov.uk/) United Kingdom's national weather service

## Glossary
- [ACORN-SAT](http://www.bom.gov.au/climate/data/acorn-sat/): Australian Climate Observations Reference Network – Surface Air Temperature
- [RAIA](http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks): Remote Australian Islands and Antarctica (a monthly dataset that is a smaller part of ACORN-SAT)
- [HadCET](https://www.metoffice.gov.uk/hadobs/hadcet/index.html): The Hadley Centre Central England Temperature (HadCET) dataset is the longest instrumental record of temperature in the world.
- [GHCNm](https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly) Global Historical Climatology Network monthly provides monthly climate summaries from thousands of weather stations around the world.

## Technical
- Built in [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/)
- C#, .NET 7, Blazor
- using
  - https://github.com/Megabit/Blazorise
  - https://github.com/DP-projects/DPBlazorMap
  - https://github.com/AeonLucid/GeoCoordinate.NetStandard1
  - https://github.com/arivera12/BlazorCurrentDevice
  - https://www.nuget.org/packages/DBSCAN/ ([source](https://github.com/viceroypenguin/Dbscan))
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
