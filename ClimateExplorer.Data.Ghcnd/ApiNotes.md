# Attributes column in the GHCNd API response format

The columns are:

- PRCP_ATTRIBUTES
- TMAX_ATTRIBUTES
- TMIN_ATTRIBUTES
- TAVG_ATTRIBUTES

When the NCEI API (and packages like GHCNr) return data, they often compress these into a single comma‑separated string:

MFLAG, QFLAG, SFLAG

This string is the three GHCNd flag fields in order:

- MFLAG (measurement flag)
- QFLAG (quality flag)
- SFLAG (source flag)

GHCNr exposes them exactly as returned by the NCEI API.

## QFLAG

If QFLAG is non‑blank, the value failed a quality‑control test and NOAA treats it as invalid for statistical use.

This is the flag that matters for:

- climatologies
- trends
- anomaly calculations
- gridding
- anything where correctness matters

NOAA’s own downstream products (nClimGrid, nClimDiv, CEI, GHCNm) drop all values with a non‑blank QFLAG.

### QFLAGs

There are fourteen possible values:

Blank = did not fail any quality assurance check\
D     = failed duplicate check\
G     = failed gap check\
I     = failed internal consistency check\
K     = failed streak/frequent-value check\
L     = failed check on length of multiday period\
M     = failed megaconsistency check\
N     = failed naught check\
O     = failed climatological outlier check\
R     = failed lagged range check\
S     = failed spatial consistency check\
T     = failed temporal consistency check\
W     = temperature too warm for snow\
X     = failed bounds check\
Z     = flagged as a result of an official Datzilla investigation

## MFLAG

Describes how the measurement was taken. Examples:

H → average of hourly values\
T → trace precipitation\
P → precipitation total from multiple days\
E → estimated value

These do not indicate bad data. They are metadata.

## SFLAG

Describes where the data came from. Examples:

S → GSOD source\
I → international exchange\
6 → US Cooperative Network\
0 → U.S. ASOS/AWOS