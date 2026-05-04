---
layout: single
title: "Feature complete"
date: 2026-04-26 15:00:00 +1000
categories: site-info
---

## Work up to April 2026

After a furious AI-assisted coding push, I believe this website is now complete. It contains most of the feautres I'd originally envisioned. Work in 2026 includes:
  
### Locations

A [locations page](https://climateexplorer.net/locations). This page lists all the locations in the database, with its heating score, data sources and a link to its climate records.

### Climate records

New climate records page. This page orders all the daily, monthly and yearly records in a table.

It also has a new type of chart: the top 100. That shows the 100 warmest (or coldest) temperature readings across all of the years. Years are plotted on the x-axis. If top 100 record occurs on a year it gets a vertical line. More readings for that year get a thicker line.

Canberra's top 100 charts are a text book example of what we'd expect if there is global heating. Recent temperatures have the higest temperatures:

![Canberra's hottest 100 daily maximum temperature records]({{site.url}}/blog/assets/canberra---hottest-100-daily-maximum-temperature-records.png)

Older records have the lowest temperatures:
![Canberra's colest 100 daily maximum temperature records]({{site.url}}/blog/assets/canberra---coldest-100-daily-maximum-temperature-records.png)

It's surprising how few locations fit this pattern even though almost all locations show warming when data is averaged.

### GHCNd

Loading [GHCNd](https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-daily) data now works correctly. I hadn't understood how quality assurance is done with GHCHd. Essentially, NOAA permits all values, no matter how ridiculous, but flags them with a QA code. ClimateExplorer now filters out all data that has a QA flag on it.

### Blog

Remade the [blog pages](https://climateexplorer.net/blog). Instead of building as a [Jekyll](https://jekyllrb.com/) static website, the blog is now server-side rendered Blazor. The main upside of this change is to simplify the technology and unify the website design.

### Performance, accessibility, search engine optimisation and work on design

ClimateExplorer uses [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) for its technology stack. For a public facing website, Blazor isn't the best choice. A static-site or using a conventional JavaScript framework (React/Vue/etc.) would be better. Over the years, however, the website has been greatly improved through caching, refactoring and newer versions of .NET. I'm fairly happy with it now.

## Where next?

I'm unsure if I'll do significant further development on ClimateExplorer. It has not found an audience despite a decent amount of work on search engine optimisation. But we're in a world where search isn't being replaced with large language models anyway. Nevertheless, it has been a rewarding journey and I'm very happy with its current state.