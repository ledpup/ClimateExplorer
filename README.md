# Climate Explorer

[Climate Explorer](https://www.climateexplorer.net/) is a website to help people understand climate change. It's focussed on trying to provide a simple and approachable interface for people to explore. It's available for Australian and New Zealand locations. The data is sourced from:

- [BOM](http://www.bom.gov.au/)
- [NIWA](https://niwa.co.nz/)
- [NOAA](https://www.noaa.gov/)

## Glossary
- [ACORN-SAT](http://www.bom.gov.au/climate/data/acorn-sat/): Australian Climate Observations Reference Network – Surface Air Temperature
- [RAIA](http://www.bom.gov.au/climate/data/acorn-sat/#tabs=Data-and-networks): Remote Australian Islands and Antarctica (a monthly dataset that is a smaller part of ACORN-SAT)
- [NIWA](https://niwa.co.nz/): New Zealand's National Institute of Water and Atmospheric Research (maintains the [7-stations](https://niwa.co.nz/seven-stations) [11-station](https://niwa.co.nz/our-science/climate/information-and-resources/nz-temp-record/temperature-trends-from-raw-data) series)
- [NOAA](https://www.noaa.gov/): National Oceanic and Atmospheric Administration

## Technical
- Built in [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/)
- C#, .NET 6
- Blazor, using
  - https://github.com/Megabit/Blazorise
  - https://github.com/DP-projects/DPBlazorMap
  - https://github.com/AeonLucid/GeoCoordinate.NetStandard1
  - https://github.com/arivera12/BlazorCurrentDevice
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
