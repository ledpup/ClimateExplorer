file name convention for areal average (aravg) time series:
ann=annual average
mon=monthly average
land_ocean=merged land-ocean surface temperature
land=land surface temperature
ocean=ocean surface temperature
latitudes=southern and northern limits of areal average
v=version number
yyyymm=date for the latest data

Annual data (aravg.ann.*) :
1st column = year
2nd column = anomaly of temperature (K)
3rd column = total error variance (K**2)
4th column = high-frequency error variance (K**2)
5th column = low-frequency error variance (K**2)
6th column = bias error variance (K**2)

Monthly data (aravg.mon.*) :
1st column = year
2nd column = month
3rd column = anomaly of temperature (K)
4th column = total error variance (K**2)
5th column = high-frequency error variance (K**2)
6th column = low-frequency error variance (K**2)
7th column = bias error variance (K**2)
8th column = diagnostic variable
9th column = diagnostic variable
10th column= diagnostic variable

NOTE: anomalies are based on the climatology from 1971 to 2000
