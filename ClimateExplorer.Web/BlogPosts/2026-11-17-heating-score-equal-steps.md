---
layout: single
title: "Heating score: how fast, in equal steps"
date: 2026-11-17 14:00:00 +1100
categories: site-info
---
The heating score now measures how fast each location is warming, not how much it has warmed, and it's now scored in ten steps of equal width. It's based on a new trend, **since 1961** until the last complete year of data (2025 at the time of writing).

## Why a rate

The heating score used to rank the warming anomaly: the average of the last 30 years minus the average of the first half of the record. That tells you how much warmer a location is than it used to be, but "used to be" depends on when the record starts. A record that starts in the 1850s compares today with a colder past than one that starts in the 1950s.

The rate of temperature increase, in °C per decade, is a better number for seeing how quickly a location's climate is changing. It can be compared directly between locations.

The warming anomaly is still on the dashboard.

## Why since 1961

A rate needs a period, and the two obvious ones don't work for ranking:

- **The full record** penalises long records. Warming sped up from about 1970, so a record that starts in 1850 averages today's rate with a century of flatter years. Every 150-year record would score low because it's long.
- **The last 30 years** is about the same period everywhere, but a 30-year trend at one location is noisy. Its uncertainty is about 0.1 °C per decade at the coast and 0.2–0.3 inland, about as large as the differences between locations. One unusually warm year at the end can move it by a whole score.

1961 is the start of the World Meteorological Organization's 1961–1990 reference period. A trend since 1961 covers the same years at every location, so record length doesn't matter. With about 65 years of data, it's about three times less noisy than a 30-year trend.

## Why equal steps

The score used to split the locations into tenths, so each score had the same number of locations. That put the fastest-warming 10% of locations, with rates from 0.43 to over 1.0 °C per decade, into a single score of 9. A 9 covered a range more than 25 times wider than a 3, and the map couldn't show the difference between a place that was warming quickly and one warming extremely quickly.

Now the range of rates is divided into ten steps of the same width, and each location gets the score of the step its rate falls in. A score means the same number of degrees everywhere on the scale. The number of locations in each score is no longer equal, and that's fine: most places have warmed at a similar rate, so most scores are in the middle.

One problem with equal steps is that a handful of extreme locations decide where the steps go. If the steps ran from the slowest rate to the fastest, 90% of locations would be crowded into four scores, and a single new station with an extreme rate would shift every boundary. So the steps run from the 1st percentile to the 99th percentile of the rates. A location faster than the top step scores 9, and one slower than the bottom step scores 0.

## How the score works

- Rates since 1961 are divided into 10 steps, each about 0.048 °C per decade wide. A 9 means warming faster than about 0.51 °C per decade, and a 0 means slower than about 0.12. The median is about 0.28.
- A location that has cooled since 1961 scores −1. These aren't part of the steps.
- To get a score, a location needs at least 30 complete years since 1961, with the first of them no later than 1965. Its data must also run to within 10 years of the most recent location's (2015 or later).

## What changed

3,953 locations have a heating score, 3,946 of them from 0 to 9. They are spread over the scores like this:

| Score | Warming rate since 1961 (°C per decade) | Locations |
| --- | --- | --- |
| 9 | 0.51 or more | 75 |
| 8 | 0.46 – 0.51 | 106 |
| 7 | 0.41 – 0.46 | 293 |
| 6 | 0.37 – 0.41 | 463 |
| 5 | 0.32 – 0.37 | 568 |
| 4 | 0.27 – 0.32 | 681 |
| 3 | 0.22 – 0.27 | 763 |
| 2 | 0.17 – 0.22 | 513 |
| 1 | 0.12 – 0.17 | 325 |
| 0 | less than 0.12 | 159 |

Compared with tenths, 1,144 locations kept their score, and 3,093 moved by no more than one. The 853 that moved by two or more all moved down by exactly two, from the old scores of 6 to 9. Fast-warming places that all used to score 9 or 8 are now spread across the top of the scale, and places that warmed at about the average rate score much as before.

The location's info panel shows its own rate and the range of rates for every score.

The [2022 post on location summary information](https://climateexplorer.net/blog/location-summary-information) describes the original method.

## Since 1961 elsewhere

- **Dashboard:** the trend field still shows the last 30 years when that trend is significant. Otherwise it shows the trend since 1961, then the full-period trend. Hover over it to see every significant trend. The trend field and the heating score can use different rates, and the heating score's info panel shows the rate it uses.
- **Chart:** adding a trend to a yearly series now draws the since-1961 trend, if the series starts by 1965 and the trend is significant. Presets and shared links keep their trends.
- **Recent Observations:** each tile shows the last 30 years, since 1961 and the early period. The full-period trend is still in About trends.
