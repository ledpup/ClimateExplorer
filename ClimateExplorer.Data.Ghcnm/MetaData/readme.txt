GLOBAL HISTORICAL CLIMATOLOGY NETWORK - MONTHLY (GHCN-M) Version 4
(Last Updated: 05/07/2019)

********************************************************************************

The underlying data, its software and all content are provided on an 'as is' 
and 'as available' basis. We do not give any warranties, whether express 
or implied, as to the suitability or usability of the product, 
its software or any of its content.

Feedback is welcome and should be sent via email to:
ncdc.ghcnm@noaa.gov

Should you have any questions or concerns regarding the product,
please let us know immediately so we can rectify these accordingly.
Your help in this regard is greatly appreciated.

********************************************************************************

1. INTRODUCTION

    1.1 OVERVIEW

        GHCN-M version 4 currently contains monthly mean temperature for over
        25,000 stations across the globe. In large part GHCN-M version 4 uses
        the same quality control and bias correction algorithms as version 3. 
        The greatest difference from previous version is a greatly expanded 
        set of stations based on the large data holdings in GHCN-Daily as well 
        as data collected as part of the International Surface Temperatue 
        Initiative databank (ISTI; Rennie et al. 2014).

        There are currently three versions of GHCN-M version 4 
        QCU: Quality Control, Unadjusted
	QCF: Quality Control, Adjusted, using the Pairwise Homogeneity
             Algorithm (PHA, Menne and Williams, 2009).
	QFE: Quality Control, Adjusted, Estimated using the Pairwise
             Homogeneity Algorithm. Only the years 1961-2010 are provided.
             This is to help maximize station coverage when calculating 
             normals. For more information, see Williams et al, 2012.  

    1.2 INTERNET ACCESS

        The GHCNM v4 product can be found here:

        FTP: ftp://ftp.ncdc.noaa.gov/pub/data/ghcn/v4

    1.3 DOWNLOADING AND INSTALLING

        WINDOWS (example):

	GHCNM files are compressed into gzip format.  For more information on gzip, 
        please see the following web site:

        www.gzip.org

        and for potential software that can compress/decompress gzip files, please see:

        www.gzip.org/#faq4


        LINUX (example):

        wget ftp://ftp.ncdc.noaa.gov/pub/data/ghcn/v4/ghcnm.latest.qcu.tar.gz
        tar -zxvf ghcnm.latest.qcu.tar.gz

        (alternatively, if "tar" does not support decompression, a user can try:

        wget ftp://ftp.ncdc.noaa.gov/pub/data/ghcn/v4/ghcnm.latest.qcu.tar.gz
        gzip -d ghcnm.latest.qcu.tar.gz
        tar -xvf ghcnm.latest.qcu.tar.gz)


        Note: the data are placed in their own separate directory, that is named
              according to the following specification:

              ghcnm.v4.x.y.YYYYMMDD where 

              x = integer to be incremented with major data additions 
              y = integer to be incremented with minor data additions
              YYYY = year specific dataset was processed and produced
              MM   = month specific dataset was processed and produced
              DD   = day specific dataset was processed and produced
 
              Two files (per element) should be present in the directory a 
              1) metadata and 2) data file. Note: there will be no 
              increments to "x" and "y" above during the phase.

2. DATA

    2.1 METADATA

       The metadata has been carried over from GHCN-Monthly v3.  This would 
       include basic geographical station information such as latitude, 
       longitude, elevation, station name, etc. The extended metadata
       information, such as surrounding vegetation, etc. was not carried 
       over, however still exists in version 3

    2.1.1 METADATA FORMAT (.inv file)

       Variable          Columns      Type
       --------          -------      ----

       ID                 1-11        Integer
       LATITUDE          13-20        Real
       LONGITUDE         22-30        Real
       STNELEV           32-37        Real
       NAME              39-68        Character

       Variable Definitions:

       ID: Station identification code. First two characters are FIPS country code

       LATITUDE: latitude of station in decimal degrees

       LONGITUDE: longitude of station in decimal degrees

       STELEV: is the station elevation in meters. -999.0 = missing.

       NAME: station name

    2.2  DATA (.dat file)

         The data within GHCNM v4 for the time being consist of monthly
         average temperature.

    2.2.1 DATA FORMAT

          Variable          Columns      Type
          --------          -------      ----

          ID                 1-11        Integer
          YEAR              12-15        Integer
          ELEMENT           16-19        Character
          VALUE1            20-24        Integer
          DMFLAG1           25-25        Character
          QCFLAG1           26-26        Character
          DSFLAG1           27-27        Character
            .                 .             .
            .                 .             .
            .                 .             .
          VALUE12          108-112       Integer
          DMFLAG12         113-113       Character
          QCFLAG12         114-114       Character
          DSFLAG12         115-115       Character

          Variable Definitions:

          ID: Station identification code. First two characters are FIPS country code

          YEAR: 4 digit year of the station record.
 
          ELEMENT: element type, monthly mean temperature="TAVG"

          VALUE: monthly value (MISSING=-9999).  Temperature values are in
                 hundredths of a degree Celsius, but are expressed as whole
                 integers (e.g. divide by 100.0 to get whole degrees Celsius).

          DMFLAG: data measurement flag, nine possible values for QCU/QCF and one for QFE:

                  Quality Controlled Unadj/Adj (QCU/QCF) Flags:

                  blank = no measurement information applicable
                  a-i = number of days missing in calculation of monthly mean
                        temperature (currently only applies to the 1218 USHCN
                        V2 stations included within GHCNM)

                  Quality Controlled Adj and estimated (QFE) Flags:
                  E = Data has been estimated from neighbors within the PHA

          QCFLAG: quality control flag, eleven possibilities within
                  quality controlled unadjusted (qcu) dataset, and three 
                  possibilities within the quality controlled adjusted (qcf) 
                  dataset.

                  Quality Controlled Unadjusted (QCU) QC Flags:
         
                  BLANK = no failure of quality control check or could not be
                          evaluated.

                  Note: the following QC checks are listed in order of execution. When
                        a value is flagged by a QC check it is not subjected to 
                        testing in subsequent checks.

                  E = Identify different stations that have the "same" 12 
                      monthly values for a given year (e.g. when all 12 months
                      are duplicated and there are at least 3 or more non-
                      missing data months, and furthermore values are considered 
                      the "same" when the absolute difference between the two values 
                      is less than or equal to 0.015 deg C)

                  D = monthly value is part of an annual series of values that
                      are exactly the same (e.g. duplicated) within another
                      calendar year in the station's record.

                  R = Flag values greater than or less than known world 
                      TAVG extremes.

                  K = Identifies and flags runs of the same value (non-missing)
                      in five or more consecutive months

                  W = monthly value is duplicated from the previous month,
                      based upon regional and spatial criteria (Note: test is only 
                      applied from the year 2000 to the present, and in general
                      only for near real time produced data sources).                   

                  I = checks for internal consistency between TMAX and TMIN. 
                      Flag is set when TMIN > TMAX for a given month. 

                  L = monthly value is isolated in time within the station
                      record and flagged, when:

                      1) a non-missing value has at least 18 missing values
                         before AND after in time., or

                      2) a non-missing value belongs to a "cluster" of 2
                         adjacent (in time) non-missing values, and the cluster of
                         values has at least 18 missing values before AND after the
                         cluster, or

                      3) a non-missing value belongs to a "cluster" of 3
                         adjacent (in time) non-missing values, and the cluster of
                         values has at least 18 missing values before AND after the
                         cluster.

                  O = monthly value that is >= 5 bi-weight standard deviations
                      from the bi-weight mean.  Bi-weight statistics are
                      calculated from a series of all non-missing values in 
                      the station's record for that particular month.

                  S = Flags value when the station z-score satisfies any of the
                      following algorithm conditions.

                      Definitions:

                      neighbor = any station within 500 km of target station.
                      zscore = (bi-weight standard deviation / bi-weight mean)
                      S(Z) = station's zscore
                      N(Z) = the set of the "5" closest non-missing neighbor zscores.
                             (Note: this set may contain less than 5 neighbors,
                             but must have at least one neighbor zscore for
                             algorithm execution)

                      Algorithm:

                      S(Z) >= 4.0 and < 5.0 and "all" N(Z) < 1.9
                      S(Z) >= 3.0 and < 4.0 and "all" N(Z) < 1.8
                      S(Z) >= 2.75 and < 3.0 and "all" N(Z) < 1.7
                      S(Z) >= 2.5 and < 2.75 and "all" N(Z) < 1.6
                      S(Z) <= -4.0 and > -5.0 and "all" N(Z) > -1.9
                      S(Z) <= -3.0 and > -4.0 and "all" N(Z) > -1.8
                      S(Z) <= -2.75 and > -3.0 and "all" N(Z) > -1.7
                      S(Z) <= -2.5 and > -2.75 and "all" N(Z) > -1.6
                                   
                  T = Identifies and flags when the temperature z-score compared 
                      to the inverse distance weighted z-score of all neighbors 
                      within 500 km (at least 2 or more neighbors are required)
                      is greater than or equal to 3.0. 
                 

                  M = Manually flagged as erroneous.

                  Quality Controlled Adjusted (QCF) QC Flags:

                  A = alternative method of adjustment used.
 
                  M = values with a non-blank quality control flag in the "qcu"
                      dataset are set to missing the adjusted dataset and given
                      an "M" quality control flag.

                  X = pairwise algorithm removed the value because of too many
                      inhomogeneities.


          DSFLAG: data source flag for monthly value

                  For more information on data source flags, please refer to
                  ghcnm-flags.txt 

3. CONTACT

    3.1 QUESTIONS AND FEEDBACK

        NCDC.GHCNM@noaa.gov
