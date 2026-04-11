---
layout: single
title: "To simplify and consolidate"
date: 2024-10-08 19:50:00 +1100
categories: datasets
---
Until recently, [ClimateExplorer](https://climateexplorer.net/) was sourcing temperature and precipitation data for locations from multiple sources. We started ClimateExplorer by using data from [Australian Climate Observations Reference Network – Surface Air Temperature (ACORN-SAT)](http://www.bom.gov.au/climate/data/acorn-sat/). It expanded to include [Remote Islands and Antarctica](http://www.bom.gov.au/climate/current/annual/ria/summary.shtml) and to New Zealand's [7-station series](https://niwa.co.nz/climate-and-weather/nz-temperature-record/seven-station-series-temperature-data) and 11-station series from the [National Institute of Water and Atmospheric Research (NIWA)](https://niwa.co.nz/). Adding these locations, one country at a time, didn't seem like a very scalable approach, but I was yet to find a suitable collated global dataset.

Now that the [Global Historical Climatology Network]({{site.baseurl}}{% post_url 2024-06-02-the-rest-of-the-world %}) is part of [ClimateExplorer](https://climateexplorer.net/), it made sense to standardise how ClimateExplorer worked for every location. To that end, the website now only has 3 datasets for location specific climate data. They are:

- GHCN: monthly mean temperature and precipitation
- ACORN-SAT: daily mean temperature and precipitation
- [HadCET](https://www.metoffice.gov.uk/hadobs/hadcet/)/HadCEP: daily mean temperature and precipitation

This change has made mean temperature the default temperature data. Maximum and minimum temperatures are still available for every site (via GHCNd for most sites), but the presets will give you mean temperature (it used to be either maximum or mean temperature, depending on the dataset).

All sites now have adjusted and unadjusted temperature records and, if available, precipitation data. If we'd kept RIA and NIWA datasets there would have been weird inconsistencies in those areas.