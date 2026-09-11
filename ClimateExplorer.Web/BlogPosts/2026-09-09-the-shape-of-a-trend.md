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

## Linear regression: a straight line through the data

A trend line here is an ordinary least-squares linear regression - the straight line through a set of yearly values that minimises the sum of the squared vertical distances from each year's value to the line. Picture each year as a point: *x* is the year, *y* is that year's value. The slope, β, comes from:

<p style="text-align:center">β = Σ(x − x̄)(y − ȳ) / Σ(x − x̄)²</p>

**x̄** and **ȳ** are the average year and average value across the record

For every year, take how far that year is from the average year, and how far its value is from the average value, and multiply the two together; add that up across every year for the sum on top. The sum on the bottom does the same thing with just the years, squared. Divide one by the other and the result is the slope - how much the fitted line rises for every extra year.

A small worked example: take the annual mean CO₂ concentration measured at Mauna Loa for 1960 to 1969, in parts per million, rounded to the nearest whole number:

| Year | ppm |
| ---: | --: |
| 1960 | 317 |
| 1961 | 318 |
| 1962 | 318 |
| 1963 | 319 |
| 1964 | 320 |
| 1965 | 320 |
| 1966 | 321 |
| 1967 | 322 |
| 1968 | 323 |
| 1969 | 325 |

The average year is 1964.5 and the average concentration is 320.3ppm. The regression finds the one straight line that best fits all ten points at once.

| Year (x) | ppm (y) | x − x̄ | y − ȳ | (x−x̄)(y−ȳ) | (x−x̄)² |
| -------: | ------: | -----: | -----: | -----------: | ------: |
| 1960 | 317 | −4.5 | −3.3 | 14.9 | 20.25 |
| 1961 | 318 | −3.5 | −2.3 | 8.1 | 12.25 |
| 1962 | 318 | −2.5 | −2.3 | 5.8 | 6.25 |
| 1963 | 319 | −1.5 | −1.3 | 2.0 | 2.25 |
| 1964 | 320 | −0.5 | −0.3 | 0.2 | 0.25 |
| 1965 | 320 | 0.5 | −0.3 | −0.2 | 0.25 |
| 1966 | 321 | 1.5 | 0.7 | 1.0 | 2.25 |
| 1967 | 322 | 2.5 | 1.7 | 4.2 | 6.25 |
| 1968 | 323 | 3.5 | 2.7 | 9.4 | 12.25 |
| 1969 | 325 | 4.5 | 4.7 | 21.1 | 20.25 |
| **Sum** | | | | **66.5** | **82.5** |

The slope is the sum on top divided by the sum on the bottom: 66.5 ÷ 82.5 ≈ **0.8 ppm per year**.

Now the most recent complete decade on record, 2016 to 2025:

| Year | ppm |
| ---: | --: |
| 2016 | 404 |
| 2017 | 407 |
| 2018 | 409 |
| 2019 | 412 |
| 2020 | 414 |
| 2021 | 416 |
| 2022 | 419 |
| 2023 | 421 |
| 2024 | 425 |
| 2025 | 427 |

Same method, mean year 2020.5 and mean concentration 415.4ppm, gives Σ(x−x̄)(y−ȳ) = 208.0 and Σ(x−x̄)² = 82.5 - the spread of years is identical. The slope is 208.0 ÷ 82.5 ≈ **2.5 ppm per year**.

The straight-line rate more than triples: 0.8 ppm per year vs 2.5 ppm per year. Reported as flat numbers, that reads like two disconnected facts about CO₂ - it rose at one pace once, it rises at a different, faster pace now - with nothing in the maths itself to say whether that's one long acceleration or two unrelated regimes. That's the real limit of ordinary least squares: it forces one constant rate of change onto whatever window you feed it, whether or not the metric behind it is actually climbing at a steady pace throughout. Recent Observations shows three overlapping windows for exactly this reason, and charts let you stack multiple trend lines: comparing them is how you notice a record that isn't behaving in a straight line at all.

## Quadratic regression: letting the line bend

A quadratic fit is the same least-squares idea extended with one more term:

<p style="text-align:center">Y = β₀ + β₁X + β₂X²</p>

The extra β₂X² term lets the curve bend instead of forcing a single rate across the whole record. Because a curve's rate of change is different at every point along it, "the slope" stops being one number - so rather than pick a single figure that would be wrong everywhere except one spot, ClimateExplorer reports the curve's **instantaneous rate of change at the most recent year**: the slope of the tangent line there. That's also why fitting the same shape of curve to two different windows of the same dataset can tell you something a straight line can't - if the tangent gets steeper as the window moves later, the data isn't just going up, it's going up faster than it used to.

### How fast is CO₂ really accelerating?

Atmospheric CO₂, measured continuously at the Mauna Loa Observatory since 1958, is a good candidate for this because the signal is so clean - it goes up practically every single year. Take the same two ten-year windows used in the linear example above - 1960 to 1969 and 2016 to 2025 - and fit a quadratic to each instead of a straight line.

Centring on the mean year again (x′ = year − 1964.5 for the first window, x′ = year − 2020.5 for the second) reuses the same trick as before: with the years evenly spaced and symmetric about the middle, Σx′ and Σx′³ both vanish. And because both windows are the same ten years wide, the two purely year-dependent sums come out identical either way: Σx′² = 82.5, Σx′⁴ = 1,208.625.

For 1960-1969, the remaining sums - computed from the same ten points as the linear example - are Σy = 3,203, Σx′y = 66.5, Σx′²y = 26,452.75. As before, the linear coefficient falls out on its own: β₁ = 66.5 ÷ 82.5 ≈ **0.81** ppm per year, matching the straight-line slope already found above. That leaves β₀ and β₂ from two equations:

<p style="text-align:center">10β₀ + 82.5β₂ = 3,203<br/>82.5β₀ + 1,208.625β₂ = 26,452.75</p>

Solving gives β₂ ≈ **0.0530** and β₀ ≈ **319.9**, so the fitted curve (with x′ = year − 1964.5) is:

<p style="text-align:center">CO₂ ≈ 319.9 + 0.81·x′ + 0.0530·x′²</p>

It fits noticeably better than the straight line did (R² = 0.982 against 0.955). The rate of change, β₁ + 2β₂·x′, at the two ends of the window: at x′ = −4.5 (1960), **0.3 ppm per year**; at x′ = +4.5 (1969), **1.3 ppm per year**. Within this single decade, CO₂'s own rate of rise more than quadruples.

Now the same method on 2016-2025 (Σy = 4,154, Σx′y = 208.0, Σx′²y = 34,284.5): β₁ ≈ **2.52** ppm per year - again matching the linear slope - and solving 10β₀ + 82.5β₂ = 4,154 and 82.5β₀ + 1,208.625β₂ = 34,284.5 gives β₂ ≈ **0.0265**, β₀ ≈ **415.2** (R² = 0.997, barely above the straight line's 0.996):

<p style="text-align:center">CO₂ ≈ 415.2 + 2.52·x′ + 0.0265·x′² (x′ = year − 2020.5)</p>

The rate of change at x′ = −4.5 (2016) is **2.3 ppm per year**; at x′ = +4.5 (2025) it's **2.8 ppm per year**.

Put the two curvature terms side by side, rather than the tangent values, and something worth noticing shows up: β₂ ≈ 0.0530 for the 1960s against β₂ ≈ 0.0265 for 2016-2025 - almost exactly half. That's a real difference, but nowhere near as extreme as the near-tripling in the straight-line slopes above (0.8 ppm per year against 2.5 ppm per year). Measured in the term that actually describes a parabola's shape - its curvature, not its height - these two decades, fifty-six years apart, look far more alike than the linear numbers alone would suggest: both are a gentle upward bow, both accelerating within the very decade being measured, just carried on top of a much higher base rate now than then.

That consistency is the physical case for a quadratic here, not just a statistical one. Atmospheric CO₂ accumulates from cumulative fossil fuel and cement emissions, and those emissions have themselves grown over time, roughly tracking a growing global economy - so the amount added to the atmosphere in a given year isn't just large, it's larger than the amount added the year before, by a growing margin. That's second-derivative behaviour: a rate of change that is itself changing over time. A straight line has no way to represent that at all; a quadratic term is the simplest curve that can. (This is a simplified illustration to show the method by hand; the live chart's own "Last 30 years" trend, described next, is fitted to a longer window than either of these two toy examples.)

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

If you want to poke at any of this yourself, every trend on a chart or in Recent Observations carries a tooltip with the underlying statistics - p-value, R², sample size - and the "About trends" panel has the same worked-through maths, using its own shorter illustrative example.
