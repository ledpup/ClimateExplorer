# ClimateExplorer

[ClimateExplorer](https://climateexplorer.net/) is a website to help people understand climate change. It's focussed on trying to provide a simple and approachable interface for people to explore the changes to climate in their region.

ClimateExplorer.net has two main sections; 1) [local climate change information](https://climateexplorer.net/) about a specific location and 2) [regional and global charts](https://climateexplorer.net/regionalandglobal) to show what is happening with greenhouse gases, ice melt, sea-level rise, ocean temperatures, etc.

This github site is the digital repository for everything used to bring [the website](https://climateexplorer.net/) together.

## Architecture and code
- Built in [Visual Studio 2026 Community Edition](https://visualstudio.microsoft.com/vs/community/) using
  - .NET 10
  - C#
  - Blazor
  - Minimal Web API
- The main projects in the solution are
  - **Web**: Blazor server-side website that displays the data to the user. This is a wrapper project, most of the Blazor files are in Web.Client.
  - **Web.Client**: Blazor Web Assembly version of the website. This will download to the browser and the browser will switch to using this after its downloaded.
  - **WebApi**: Web API that gets and processes the data that the website uses
  - **Core**: code shared between Web, WebApi and other projects
  - **UnitTests**: tests for various sub-systems
- Additional libraries used
  - Charting: https://github.com/Megabit/Blazorise
  - Mapping: https://github.com/ledpup/DPBlazorMap - a fork of https://github.com/DP-projects/DPBlazorMap with a minor compatibility change to support .NET 8 and beyond
  - Clustering tool: https://www.nuget.org/packages/DBSCAN/ ([source](https://github.com/viceroypenguin/Dbscan))
  - https://www.nuget.org/packages/Blazored.LocalStorage/ ([source](https://github.com/Blazored/LocalStorage))
  - https://github.com/AeonLucid/GeoCoordinate.NetStandard1
  - https://github.com/HugoVG/CurrentDevice

## How to use

- Download the github repo. 
- Open ClimateExplorer.sln in Visual Studio 2026. 
- Set your start-up projects to be **Web** and **WebApi** and run.
  - Two websites should start-up; the user interface and the web API.