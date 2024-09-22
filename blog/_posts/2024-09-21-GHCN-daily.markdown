---
layout: single
title: "Global Historical Climatology Network daily"
date: 2024-09-21 21:00:00 +1000
categories: datasets
---
After we added [The rest of the world]({{site.baseurl}}{% post_url 2024-06-02-the-rest-of-the-world %}) (i.e., adding sites from around the world, sourced from [Global Historical Climatology Network monthly (GHCNm)](https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-monthly)), I thought it would be worthwhile to acquire the daily data for those sites from the [Global Historical Climatology Network daily (GHCNd)](https://www.ncei.noaa.gov/products/land-based-station/global-historical-climatology-network-daily).

Initially, this seemed like a daunting task. We had selected 1883 sites from GHCNm. GHCNd data files for those sites total over 7 gigabytes. I wanted it to be a few hundred megabytes.

The first thing to do would be to transform the GHCNd file into something that [Climate Explorer](https://climateexplorer.net/) could use. The data file format in GHCNd looks like the below.

```
"STATION","DATE","LATITUDE","LONGITUDE","ELEVATION","NAME","PRCP","PRCP_ATTRIBUTES","SNWD","SNWD_ATTRIBUTES","TMAX","TMAX_ATTRIBUTES","TMIN","TMIN_ATTRIBUTES","TAVG","TAVG_ATTRIBUTES"
"AG000060590","1892-01-01","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   34",",,E",,
"AG000060590","1892-01-02","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   36",",,E",,
"AG000060590","1892-01-03","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   30",",,E",,
"AG000060590","1892-01-04","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   40",",,E",,
"AG000060590","1892-01-05","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   30",",,E",,
"AG000060590","1892-01-06","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   24",",,E",,
"AG000060590","1892-01-07","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   38",",,E",,
"AG000060590","1892-01-08","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   84",",,E",,
"AG000060590","1892-01-09","30.5667","2.8667","397.0","EL GOLEA, AG","    0",",,E",,,,,"   40",",,E",,
```

ClimateExplorer will only use max/min temperature and precipitation from GHCNd. However, we found that precipitation and temperature data are not correlated. A site may have records for one and not the other. It makes sense to separate the two types of data into different files. We transformed the file above into a temperature file...

```
Date,TMax,TMin
19400101,202,44
19400102,212,62
19400103,232,70
19400104,231,106
19400105,202,80
19400106,198,65
19400107,218,72
19400108,222,70
19400109,213,76
```

... and a precipitation file.

```
Date,Precipitation
18920101,0
18920102,0
18920103,0
18920104,0
18920105,0
18920106,0
18920107,0
18920108,0
18920109,0
```

There is a check to simply discard a row (i.e., a day of data) if all of the measurements are not present. There is also a filter to discard a site if it doesn't have at least 10 years of data with over 300 days of data for each of those years.

The code for downloading and reducing the data to be suitable for ClimateExplorer is at [ClimateExplorer.Data.Ghcnd](https://github.com/ledpup/ClimateExplorer/tree/master/ClimateExplorer.Data.Ghcnd).