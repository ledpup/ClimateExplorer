This set of files contains the following:

- homogenised ACORN-SAT data
- raw station data for stations corresponding to the ACORN-SAT locations
- a file (primarysites.txt) with information on which site is the primary site for each ACORN-SAT occasion and for which period of time
(in most cases, there will be two or three primary sites which make up the input data for the overall ACORN-SAT record)
- a summary of adjustments and reference periods and stations. This information is also contained in the station catalogue.
- each of the transfer functions for these adjustments.

Format of raw data files

These file names have the file name hqnewNNNNNN, where NNNNNN is the station number.

Each day of data has the format:

NNNNNN YYYYMMDD  XXX  NNN

where NNNNNN is the station number, YYYYMMDD is the date, XXX is the maximum temperature in tenths of degrees C
(e.g. 251 = 25.1 C) and NNN is the minimum temperature. Missing data is shown as -999.

These data files have been quality controlled but not adjusted.

Format of primarysites.txt

This file takes the form

NNNNNN  N1N1N1 YYYYMMDD YYYYMMDD N2N2N2 YYYYMMDD YYYYMMDD N3N3N3 YYYYMMDD YYYYMMDD

NNNNNN denotes the current station number (used in the adjusted ACORN-SAT data). N1N1N1, N2N2N2 and N3N3N3 are the site numbers
which make up the ACORN-SAT record, and the listed date range is the period when they were the primary site.

Where there are only 1 or 2 stations used for the ACORN-SAT record, the remaining columns are filled with 999999/99999999.

Format of transfer function files

The transfer function files have the name transfuncNNNNNNYYYYc, where:

NNNNNN is the station number (usually corresponding to the primary site at the time)
YYYY is the year of the adjustment
c is the code for the variable being adjusted (x for maximum, n for minimum). If it is blank the adjustment is applied to
both maximum and minimum. 'sp' denotes a spike correction.

The file format is as follows:

Column 1 - original value (in 0.1 degrees)
Columns 2-13 - adjusted maximum temperature values for January-December
Columns 14-25 - adjusted minimum temperature values for January-December

Note that although both maximum and minimum adjusted values are shown, in most cases only one of the two sets will be implemented 
(shown by the suffix on the file name)

6 April 2020