Original file names are:
- acorn_sat_v2.2.0_daily_tmax.tar.gz
- acorn_sat_v2.2.0_daily_tmin.tar.gz
- acorn_sat_v2.1.0_daily_tmax.tar.gz
- acorn_sat_v2.1.0_daily_tmin.tar.gz

Raw data file is:
- raw-data-and-supporting-information.zip


Files sourced from:
- ftp://ftp.bom.gov.au/anon/home/ncc/www/change/ACORN_SAT_daily/
- Accessed 09/12/2021


Adjustments:
The tmax and tmin file lengths aren't all the same. Some start on different dates to others, when there are missing records at the start or the end. It's usually just a few days. I adjuested the files so they all start and end on the same day to the files below. Explained in detailed for Esperance. All the others are similar, trivial adjustments.

- Esperance,009789; tmax daily file starts on 1910-01-03. tmin starts on 1910-01-02. Added a null 1910-01-02 row for tmax
- Forrest,011052
- Gabo Island,084016
- Low Head,091293
- Normanton,029063
- Oodnadatta,017043
- Orbost,084145
- Rockhampton,039083