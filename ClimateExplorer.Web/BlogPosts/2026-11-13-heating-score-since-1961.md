---
layout: single
title: "Heating score: how fast, not how much"
date: 2026-11-13 14:00:00 +1100
categories: site-info
---
The heating score now ranks how fast each location is warming, not how much it has warmed. It's based on a new trend, **since 1961** until the last complete year of data (2025 at the time of writing).

## Why a rate

The heating score used to rank the warming anomaly: the average of the last 30 years minus the average of the first half of the record. That tells you how much warmer a location is than it used to be, but "used to be" depends on when the record starts. A record that starts in the 1850s compares today with a colder past than one that starts in the 1950s.

The rate of temperature increase, in °C per decade, is a better number for seeing how quickly a location's climate is changing. It can be compared directly between locations.

The warming anomaly is still on the dashboard.

## Why since 1961

A rate needs a period, and the two obvious ones don't work for ranking:

- **The full record** penalises long records. Warming sped up from about 1970, so a record that starts in 1850 averages today's rate with a century of flatter years. Every 150-year record would score low because it's long.
- **The last 30 years** is about the same period everywhere, but a 30-year trend at one location is noisy. Its uncertainty is about 0.1 °C per decade at the coast and 0.2–0.3 inland, about as large as the differences between locations. One unusually warm year at the end can move it by a whole score.

1961 is the start of the World Meteorological Organization's 1961–1990 reference period. A trend since 1961 covers the same years at every location, so record length doesn't matter. With about 65 years of data, it's about three times less noisy than a 30-year trend.

## How the score works

- Locations are ranked on their warming rate since 1961 and split into tenths. The fastest-warming 10% score 9 and the slowest 10% score 0. A 9 currently means warming faster than about 0.43 °C per decade, and a 0 slower than about 0.16. The median is about 0.28.
- A location that has cooled since 1961 scores −1.
- To get a score, a location needs at least 30 complete years since 1961, with the first of them no later than 1965. Its data must also run to within 10 years of the most recent location's (2015 or later).

## What changed

3,953 locations now have a heating score. The old method would have scored 3,484 of them, because the warming anomaly needs 60 years of records. About a third of locations moved by 2 or more. The biggest moves were:

- **Down:** long records with a cold early period, which had a large anomaly but an ordinary rate since 1961. Buenos Aires (records from 1856) went from 9 to 1, and Hiroshima and Vladivostok from 9 to 2.
- **Up:** places with flat early records that have warmed strongly since 1961. Savannah and Tasiilaq went from 0 to 7.

The [2022 post on location summary information](https://climateexplorer.net/blog/location-summary-information) describes the original method.

## Since 1961 elsewhere

- **Dashboard:** the trend field still shows the last 30 years when that trend is significant. Otherwise it shows the trend since 1961, then the full-period trend. Hover over it to see every significant trend. The trend field and the heating score can use different rates, and the heating score's info panel shows the rate it uses.
- **Chart:** adding a trend to a yearly series now draws the since-1961 trend, if the series starts by 1965 and the trend is significant. Presets and shared links keep their trends.
- **Recent Observations:** each tile shows the last 30 years, since 1961 and the early period. The full-period trend is still in About trends.
