# Introduction

ClimateExplorer data is updated yearly, usually from mid-January. All data is sourced from the internet. This file describes the update process to retrieve the data for the previous year. The process is broken-down into three sections, though they could be executed as one batch. Building and running ClimateExplorer.DataPipeline will get all the updated data into the web API as ZIP files. The long-term cache can then be re-generated (ClimateExplorer.CachingTool) as the final step.

## Update regional and global data:

1. Run **ClimateExplorer.Data.Misc**
1. Manually update latest CO2 reported emissions file (it doesn't have a fixed URL, unfortunately). Go to https://zenodo.org/records/10562476 to find the latest file.
1. Build **ClimateExplorer.DataPipeline** (zips up the updated data folders, residing in ClimateExplorer.SourceData, and moves them to ClimateExplorer.WebApi)

## Update BOM data:

1. If a new version of ACORN-SAT has been released, update the version number for the FTP download in the AcornSatDownloader.DownloadAndExtractData function in ClimateExplorer.Data.Bom
1. Run **ClimateExplorer.Data.Bom**
1. Run **ClimateExplorer.Data.Bom.CreateTempMean** (this will calculate a mean record from the min and max records downloaded in ClimateExplorer.Data.Bom
1. Optionally run **ClimateExplorer.Data.Bom.AcornSatUpdateFromRaw** (do this step if doing the update at the start of the year, not at the time of a new version of ACORN-SAT)
1. Build **ClimateExplorer.DataPipeline** (zips up the records for use by ClimateExplorer.WebApi)

## Update GHCN data:

1. Clear the Output folder from **ClimateExplorer.Data.Ghcnm** (it has cached files that it'll re-use if you miss this step, so you won't get the latest data). This is only required if you ran ClimateExplorer.Data.Ghcnm for last year
1. Run **ClimateExplorer.Data.Ghcnm** (this downloads all of GHCNm and then selects the most appriopriate sites using a clustering algorithm)
1. Run **ClimateExplorer.Data.Ghcnm.Precipitation** (gets precipitation for the sites selected as part of ClimateExplorer.Data.Ghcnm)
1. Run **ClimateExplorer.Data.Ghcnd** (gets the daily temperature and precipitation for the sites selected as part of ClimateExplorer.Data.Ghcnm)
1. Build **ClimateExplorer.DataPipeline** (will update Temperature.zip and Precipitation.zip)
1. Run **ClimateExplorer.DataPipeline** (will create the GCHNd zip files - CachingTool will almost certainly break if you forget this step)
1. Run **ClimateExplorer.WebApi**
    - Run **ClimateExplorer.CachingTool** (re-creates all the long-term cache files; requires ClimateExplorer.WebApi to be running)