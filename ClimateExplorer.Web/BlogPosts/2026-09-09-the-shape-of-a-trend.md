---
layout: single
title: "Projection of the future"
date: 2026-09-09 09:00:00 +1000
categories: site-info
---

Recent work on this website has added trend calculations to project future values of temperatures and other metrics. The **recent observations** panel on every location page reports a rate of change per decade for temperature and rainfall. Every chart can have up to three trend lines per series. There are built-in presets - a per-location temperature trend, and a global CO₂ trend. All of it rests on the same two pieces of maths: **linear regression** (the straight line through a set of points) and **quadratic regression** (letting that line bend into a curve). This post explains how they're calculated.

## Where you'll find it

The **Trend** tab in Recent Observations fits a straight line to a metric's yearly history and reports the slope as a rate per decade, for three overlapping windows: the **full period of record**, the **last 30 years**, and the **first half of the record**. Comparing the three is a quick check for acceleration - if the last-30-years rate is steeper than the early-period rate, the metric isn't just trending, it's trending faster than it used to. A trend only gets reported at all once it clears a statistical significance bar; short of that, the tile plainly says "No significant trend" rather than printing a number that isn't reliably different from noise. There's a much fuller explanation of all of this, worked example included, behind the "About trends" link on that tab.

Charts go further. Any chart series can carry up to three trend lines, each independently configured: **linear**, **quadratic**, or **cubic**; fitted to the full record, the last 30 years, or the first half; and projected forward by a fixed number of years or out to a target year. We deliberately support more than one trend per series, because a trend line is a possible future, not a certainty - showing three different fits side by side (say, the full-record trend against the last-30-years trend) is a better answer to "where is this headed?" than picking one and presenting it as the answer.

Two chart presets use this. **Temperature + trend**, on every location page, fits both a linear and a quadratic trend to the full temperature record and projects 50 years out. **Atmospheric CO₂ vs emissions**, on the [global page](https://climateexplorer.net/), is the one we'll spend the second half of this post on.

## Linear regression: the straight line through the data

A trend line here is an ordinary least-squares linear regression - the straight line through a set of yearly values that minimises the sum of the squared vertical distances from each year's value to the line. Picture each year as a point: *x* is the year, *y* is that year's value. The slope, β, comes from:

<p style="text-align:center">β = Σ(x − x̄)(y − ȳ) / Σ(x − x̄)²</p>

where x̄ and ȳ are the average year and average value across the record. For every year, take how far that year is from the average year, and how far its value is from the average value, and multiply the two together; add that up across every year for the sum on top. The sum on the bottom does the same thing with just the years, squared. Divide one by the other and the result is the slope - how much the fitted line rises for every extra year.

A small worked example: say a station has five years of data - 2001 at 14.00°C, 2002 at 14.04°C, 2003 at 14.12°C, 2004 at 14.09°C, 2005 at 14.20°C. The average year is 2003 and the average temperature is 14.09°C. Real records rarely rise by exactly the same amount every year - here 2003 sits a little above where a perfectly steady rise would put it, and 2004 sits a little below - but the regression still finds the one straight line that best fits all five points at once.

| Year (x) | Temp °C (y) | x − x̄ | y − ȳ | (x−x̄)(y−ȳ) | (x−x̄)² |
| -------: | ----------: | -----: | -----: | -----------: | ------: |
| 2001 | 14.00 | −2 | −0.09 | 0.18 | 4 |
| 2002 | 14.04 | −1 | −0.05 | 0.05 | 1 |
| 2003 | 14.12 | 0 | 0.03 | 0.00 | 0 |
| 2004 | 14.09 | 1 | 0.00 | 0.00 | 1 |
| 2005 | 14.20 | 2 | 0.11 | 0.22 | 4 |
| **Sum** | | | | **0.45** | **10** |

The slope is the sum on top divided by the sum on the bottom: 0.45 ÷ 10 = 0.045°C per year. Multiply by ten to get a per-decade rate small numbers like this would otherwise obscure, and that's the **+0.45°C per decade** a Recent Observations tile would display. This is the exact example now shown in the "About trends" panel in the app, if you'd like to see it alongside the real thing.

Ordinary least squares forces one constant rate of change onto the whole record, which is exactly why Recent Observations shows three separate windows rather than a single number, and why charts let you stack multiple trend lines: comparing them is how you notice a record that isn't behaving in a straight line at all.

## Quadratic regression: letting the line bend

A quadratic fit is the same least-squares idea extended with one more term:

<p style="text-align:center">Y = β₀ + β₁X + β₂X²</p>

The extra β₂X² term lets the curve bend instead of forcing a single rate across the whole record. Because a curve's rate of change is different at every point along it, "the slope" stops being one number - so rather than pick a single figure that would be wrong everywhere except one spot, ClimateExplorer reports the curve's **instantaneous rate of change at the most recent year**: the slope of the tangent line there. That's also why fitting the same shape of curve to two different windows of the same dataset can tell you something a straight line can't - if the tangent gets steeper as the window moves later, the data isn't just going up, it's going up faster than it used to.

### How fast is CO₂ really accelerating?

Atmospheric CO₂, measured continuously at the Mauna Loa Observatory since 1958, is a good candidate for this because the signal is so clean - it goes up practically every single year. Here's the annual mean CO₂ concentration for 1970 to 2000, in parts per million, rounded to the nearest whole number:

| Year | ppm | Year | ppm | Year | ppm | Year | ppm |
| ---: | --: | ---: | --: | ---: | --: | ---: | --: |
| 1970 | 326 | 1978 | 335 | 1986 | 348 | 1994 | 359 |
| 1971 | 326 | 1979 | 337 | 1987 | 349 | 1995 | 361 |
| 1972 | 327 | 1980 | 339 | 1988 | 352 | 1996 | 363 |
| 1973 | 330 | 1981 | 340 | 1989 | 353 | 1997 | 364 |
| 1974 | 330 | 1982 | 341 | 1990 | 354 | 1998 | 367 |
| 1975 | 331 | 1983 | 343 | 1991 | 356 | 1999 | 369 |
| 1976 | 332 | 1984 | 345 | 1992 | 357 | 2000 | 370 |
| 1977 | 334 | 1985 | 346 | 1993 | 357 | | |

With 31 years of data, multiplying out every row by hand isn't practical the way it was for five - but the years are evenly spaced and symmetric around their midpoint, 1985, so centring X on the mean year (x′ = year − 1985) makes most of the arithmetic cancel on its own: any odd power of a symmetric range sums to zero, so Σx′ = 0 and Σx′³ = 0. What's left, computed once from the whole table, is:

- Σx′² = 2,480, Σx′⁴ = 356,624
- Σy = 10,741, Σx′y = 3,723, Σx′²y = 860,257

Because Σx′ and Σx′³ vanish, the linear coefficient falls straight out on its own: β₁ = Σx′y ÷ Σx′² = 3,723 ÷ 2,480 ≈ **1.5012** ppm per year. That leaves just two unknowns, the constant β₀ and the curvature β₂, from two equations:

<p style="text-align:center">31β₀ + 2,480β₂ = 10,741<br/>2,480β₀ + 356,624β₂ = 860,257</p>

Solving that pair gives β₂ ≈ **0.006175** and β₀ ≈ **345.99**. So the fitted curve (with x′ = year − 1985) is:

<p style="text-align:center">CO₂ ≈ 345.99 + 1.5012·x′ + 0.006175·x′²</p>

It fits the 31 points closely (R² = 0.997). The interesting part is the rate of change, β₁ + 2β₂·x′, evaluated at each end of the window. At x′ = −15 (year 1970): 1.5012 − 0.1852 = 1.316 ppm/year → **13.2 ppm per decade**. At x′ = +15 (year 2000): 1.5012 + 0.1852 = 1.687 ppm/year → **16.9 ppm per decade**. The same curve says CO₂ was already accelerating over this window, from about 13 to about 17 ppm per decade.

Now the last ten years:

| Year | ppm | Year | ppm | Year | ppm | Year | ppm | Year | ppm |
| ---: | --: | ---: | --: | ---: | --: | ---: | --: | ---: | --: |
| 2016 | 404 | 2018 | 409 | 2020 | 414 | 2022 | 419 | 2024 | 425 |
| 2017 | 407 | 2019 | 412 | 2021 | 416 | 2023 | 421 | 2025 | 427 |

Same method, mean year 2020.5, gives Σx′² = 82.5, Σx′⁴ = 1,208.625, Σy = 4,154, Σx′y = 208, Σx′²y = 34,284.5. The linear term is again immediate: β₁ = 208 ÷ 82.5 ≈ **2.5212** ppm/year. The remaining pair, 10β₀ + 82.5β₂ = 4,154 and 82.5β₀ + 1,208.625β₂ = 34,284.5, solves to β₂ ≈ **0.026515** and β₀ ≈ **415.18** (R² = 0.997 again). The rate of change at the start of this window (2016, x′ = −4.5) is 2.283 ppm/year → **22.8 ppm per decade**; at the end (2025, x′ = +4.5) it's 2.760 ppm/year → **27.6 ppm per decade**.

Put the two fits side by side and the acceleration is stark: the curve fitted to 1970-2000 says the "current rate" as of 2000 was about **17 ppm per decade**. The curve fitted to the last ten years says the current rate as of 2025 is about **28 ppm per decade** - well over half as fast again, in the space of a single generation. (This is a simplified illustration to show the method by hand; the live chart's own "Last 30 years" trend, described next, is fitted to a longer and more recent window than either of these two toy examples.)

## The CO₂ preset, and a real discrepancy

The **Atmospheric CO₂ vs emissions** preset plots two series together: CO₂ measured in the atmosphere at Mauna Loa (brown), and reported global CO₂ emissions from the Global Carbon Project (black). The atmosphere series carries a quadratic trend fitted to its last 30 years of data, projected out to 2100; the emissions series carries two, one over the full record and one over its last 30 years, both projected the same way.

![CO2 trends for atmosphere and reported emissions, projected to 2100]({{site.url}}/blog/assets/carbon-dioxide-trends.png)

*Atmospheric CO₂ (brown) and reported CO₂ emissions (black), each with a quadratic trend fitted to the last 30 years of data (ending 2025) and projected to 2100. The atmospheric trend predicts CO₂ concentrations over 730ppm by 2100 (up from 429ppm in 2025). The reported-emissions trend, fitted the same way, predicts emissions going negative well before then - a physical impossibility. A purely mathematical curve fit has no concept of a floor at zero, so nothing stops it projecting negative values for a quantity - emissions, or rainfall, or ice extent - that can never actually go below zero.*

Set the implausible negative-emissions projection aside for a moment and look at what the two trends are actually saying about *now*, not 2100: atmospheric CO₂'s rate of increase is accelerating, and reported emissions appear to be flattening out. Those are opposite directions for numbers that, naively, ought to move together - more emitted should mean more measured. A few reasons a gap like this can open up:

- **A direct measurement versus a modelled estimate.** Mauna Loa's reading is air sampled and analysed on-site. Reported national emissions are built from energy-use statistics, industrial data and modelling assumptions, and are revised for years after first publication - the most recent few years of almost any emissions inventory are its least certain, and the most likely to be revised upwards later.
- **Scope.** The reported-emissions series here is fossil fuel and cement CO₂ specifically, not the full carbon budget. Land-use change - deforestation and agriculture - is a separately estimated, still-substantial source that isn't part of this line.
- **Weakening sinks, not just changing sources.** Only roughly half of the CO₂ humanity emits in a given year stays in the atmosphere; land ecosystems and the ocean absorb the rest. If those sinks are becoming less effective as the planet warms - drought-stressed forests, marine heatwaves, ocean carbon chemistry - a larger share of the *same* emissions ends up staying airborne. That alone would show up as atmospheric CO₂ accelerating even against flat emissions.
- **Natural variability layered on top.** El Niño years are well known to produce a bigger jump in atmospheric CO₂, because tropical drought and fire reduce how much carbon land ecosystems soak up that year - independent of any trend in emissions.

None of that means either curve is "right" and the other "wrong" - it means a straight mathematical extrapolation of either one, 75 years into the future, is leaning on an assumption (today's rate of change holding indefinitely) that a 30-year window can't itself confirm. That's the same limitation the app's own trend explainer calls out for curved fits: the shorter and more recent the window, the harder that assumption has to work.

## Why not cubic?

The regression tool in ClimateExplorer supports one degree beyond quadratic - a cubic fit, Y = β₀ + β₁X + β₂X² + β₃X³, which can bend one way and then the other. It's a real shape some records have, and it's available if you want it. It's also the most flexible of the three shapes on offer, and flexible curves fit noise readily - with four parameters to estimate instead of three, a cubic needs a longer, cleaner record before its extra bend is describing something real rather than the particular years it happened to be fitted to. We don't use it in either built-in preset, and we'd suggest reaching for it deliberately rather than by default: treat a barely-significant cubic fit with more scepticism than a barely-significant line or curve.

If you want to poke at any of this yourself, every trend on a chart or in Recent Observations carries a tooltip with the underlying statistics - p-value, R², sample size - and the "About trends" panel has the full worked-through maths, including the example above.
