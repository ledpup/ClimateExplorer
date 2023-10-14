This is a brief readme file for  Global Historical Climatology Network 
(GHCN) Monthly Precipitation Version 4 (GMP4).

Directory and file organization:
doc/
ghcn-m_v4_prcp_inventory.txt
ghcn-m_v4_prcp_readme.txt (this file)

access/
Over 100,000 individual CSV files

archive/
A single tar/gzipped file containing the data files

---

The ghcn-m_v4_prcp_inventory.txt file contains the most recent location and
name associated with each GHCN ID.  These locations may vary within the 
individual CSV files as source metadata changes.
Field	Column	Contents
1	 1-11	GHCN identifier (see below for decoding)
2	13-20	Latitude	
3	22-30	Longitude	
4	32-37	Elevation (meters)
5	39-40	State/province, if available.
6	42-79	Station name	
7	81-85	WMO ID, if available.  99999=missing
8	87-90	First year in record
9	92-95	Last year in record

The comma separated value data file for each location is also fixed width.
Fields, column ranges, and their contents are as follows:

Field	Column	Contents
1	1-11	GHCN identifier (see below for decoding)
2	13-52	Station name
3	54-62	Latitude
4	64-73	Longitude
5	75-82	Elevation (meters)
6	84-89	4 digit year and 2 digit month
7	91-96	Precipitation value (tenths of a millimeter). Trace value is -1.
8	98	Measurement flag
9	100	Quality control flag
10	102	Source flag
11	104-109	Source index

Measurement flags used here only indicate the number of days missing from a 
monthly total derived from daily values.  A blank flag may indicate that 
either no values were missing, or that the data came from a monthly source 
and no information about missing days is available.  Flag values 
A-E correspond to 1-5 days missing from the month.

---------------------
Quality Control Flags
---------------------
Flag	Definition
D	The monthly value is part of a sequence of 6 or more values duplicated 
	in another portion of this record.
K	The monthly value is part of a streak of constant values.  This streak 
	may be from one month to the next, or a constant value in a particular 
	month from one year to the next.
L	No other values exist within 18 months of this value.
O	The value is a statistical outlier.
R	The value exceeds a known world record, includes negative values except -1.
S	For GHCN daily sourced records only, the number of nonzero precipitation 
        days is grossly inconsistent with the number at neighboring stations 
T	The value at this location is either much smaller or much larger than 
	neighboring values.
W	Within this country, at over half of the locations, this month and an 
	adjacent month have identical values.

------------
Source Flags
------------
Flag	Definition
D	All sources within GHCN daily except Global Summary of the Day
H	US Historical Climatological Network (USHCN)
M	Monthly Climatic Data for the World (MCDW)
R	International Collection
S	Global Summary of the Day (GSOD)
W	World Weather Records (WWR)
Z	Datzilla fix

---------------
GHCN Identifier
---------------
Characters 1-2  : Country code as in https://www.ncei.noaa.gov/data/ghcnm/v4/doc/ghcnm-countries.txt
                  with the following additions: MN-Monaco, NN-St. Maarten, UC-Curacao, 
                  VI-British Virgin Islands, XX-Unassigned.

Character   3   : Network code as in Section IV of https://ncei.noaa.gov/pub/data/ghcn/daily/readme.txt
                  Currently network codes is X if not provided
Characters 4-11 : Varies depending on network code.  If characters 3-5 are XLP the
                  number that follows is an internal index.  If characters 3-5 are
                  MLP, an asserted but unofficial WMO number is used.

-------------------
Contact Information
-------------------
Please e-mail questions, suggestions, or other feedback to ncei.ghcnm.precipitation@noaa.gov.
