---
layout: single
title: "Feature complete"
date: 2026-04-26 15:00:00 +1000
categories: site-info
---

## Work up to April 2026

After a furious AI-assisted coding push, I think this website is now feature-complete. It includes most of the features I originally envisioned. Work completed in 2026 includes:
  
### Locations

A new [locations page](https://climateexplorer.net/locations) lists every location in the database, along with its heating score, data sources, and a link to its climate records.

### Climate records

There is also a new climate records page, which presents daily, monthly, and yearly records in a table.

It also introduces a new chart type: the Top 100. This shows the 100 warmest or coldest temperature readings across all years. Years are plotted on the x-axis. If a top 100 record occurs in a given year, that year receives a vertical line. Multiple readings in the same year make the line thicker.

Canberra's Top 100 charts are a textbook example of what we would expect in a warming climate: recent years contain the highest temperatures.

![Canberra's hottest 100 daily maximum temperature records]({{site.url}}/blog/assets/canberra---hottest-100-daily-maximum-temperature-records.png)

Older records contain the lowest temperatures.
![Canberra's coldest 100 daily maximum temperature records]({{site.url}}/blog/assets/canberra---coldest-100-daily-maximum-temperature-records.png)

What is surprising is how few locations fit this pattern, even though almost all locations show warming when the data is averaged.

### GHCNd

Loading [GHCNd](https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-daily) data now works correctly. I had not fully understood how quality assurance works in GHCNd. Essentially, NOAA keeps all values, no matter how implausible, but flags suspect observations with a QA code. ClimateExplorer now filters out all data that carries a QA flag.

### Blog

The [blog pages](https://climateexplorer.net/blog) have also been rebuilt. Instead of using a [Jekyll](https://jekyllrb.com/) static site, the blog is now server-side rendered in Blazor. The main benefit is a simpler technology stack and a more unified site design.

### Performance, accessibility, search engine optimisation, and design

ClimateExplorer uses [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) as its technology stack. For a public-facing website, Blazor is not the most obvious choice. A static site or a conventional JavaScript framework such as React or Vue would probably be a better fit. Over the years, however, the site has improved significantly through caching, refactoring, and newer versions of .NET. I am fairly happy with where it has landed.

## Where next?

I am unsure whether I will do much more significant development on ClimateExplorer. Despite a decent amount of work on search engine optimisation, it has not found much of an audience. But search itself is changing as large language models reshape how people discover information. Regardless, building ClimateExplorer has been rewarding, and I am very happy with its current state.