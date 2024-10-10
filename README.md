# ClimateExplorer

[ClimateExplorer](https://climateexplorer.net/) is a website to help people understand climate change. It's focussed on trying to provide a simple and approachable interface for people to explore the changes to climate in their region. This github site is the digital repository for everything used to bring [the website](https://climateexplorer.net/) together.

## Architecture and code
- Built in [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/) using
  - .NET 8
  - C#
  - Blazor
  - Minimal Web API
- The main projects in the solution are:
  - **Web**: Blazor server-side website that displays the data to the user. This is a wrapper project, most of the Blazor files are in Web.Client.
  - **Web.Client**: Blazor Web Assembly version of the website. This will download to the browser and the browser will switch to using this after its downloaded.
  - **WebApi**: Web API that gets and processes the data that the website uses
  - **Core**: code shared between Web, WebApi and other projects
  - **UnitTests**: tests for various sub-systems
- Additional libraries used
  - https://github.com/Megabit/Blazorise
  - https://github.com/DP-projects/DPBlazorMap
  - https://github.com/AeonLucid/GeoCoordinate.NetStandard1
  - https://github.com/HugoVG/CurrentDevice
  - https://www.nuget.org/packages/DBSCAN/ ([source](https://github.com/viceroypenguin/Dbscan))
  - https://www.nuget.org/packages/Blazored.LocalStorage/ ([source](https://github.com/Blazored/LocalStorage))

## How to use

- Download the github repo. 
- Open in Visual Studio 2022. 
- Set your start-up projects to be **Web** and **WebApi** and run.
  - Two websites should start-up; the user interface and the web API.
