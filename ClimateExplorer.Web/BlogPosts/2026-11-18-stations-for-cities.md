---
layout: single
title: "Stations for cities"
date: 2026-11-18 14:00:00 +1100
categories: datasets
---
Five days ago, [Adding more stations to the website](https://climateexplorer.net/blog/more-stations) described new rules for choosing which weather stations are on ClimateExplorer. Those rules have been changed again. This post explains what changed and why.

## Geelong, Wollongong and Bendigo

The last post ended with a gap. Geelong, Wollongong and Bendigo are places people search for, but none of their stations has a long enough record for the long-record rules. The isolation rule didn't add them either, because each town is within 75 km of an existing location. Geelong's nearest was Laverton, Melbourne's station, 48 km away.

The isolation rule asks "is this station far from other stations?". The new rule asks a different question: "does this city have a station?"

## The city rule

The rule uses a list of the world's cities and their populations from [GeoNames](https://www.geonames.org/). For each city of 50,000 people or more, largest first:

1. If a location in the same country is within 30 km of the city centre, the city is covered and nothing is added.
2. Otherwise, the best station within 25 km of the centre is added, if it's still reporting and isn't within 10 km of another location.
3. The location is named after the city, with the station in brackets: [Geelong (Avalon Airport)](https://climateexplorer.net/location/geelong), [Wollongong (Bellambi AWS)](https://climateexplorer.net/location/wollongong) and [Bendigo (Bendigo Airport)](https://climateexplorer.net/location/bendigo).

767 locations are now named after a city, among them Karachi, Johannesburg, Rio de Janeiro, Toronto and Dubai. Some of these were already on the site under their station's name; the rest are new. Search finds them by either name.

## One rule for recent data

While checking the results, we found that the recency rules let in stations that had stopped reporting years ago. A single missing month makes a year unusable, so a station with a few scattered months since 2017 has nothing to offer. The four different recency rules are now one, applied to every station: at least **2 complete years in the last 10** (2016–2025).

Together, the two changes removed 424 stations and added 390. ClimateExplorer now has 5,501 locations.

## Is this about urban heat?

Cities are warmer than the countryside around them. This is the urban heat island effect, and it's real. Someone who doubts climate change might see a rule that adds stations in cities and conclude the aim is to push the numbers up.

It isn't. The city rule is for people. Someone who comes to ClimateExplorer wants to see the climate where they live, and most people live in or near a city. A station 50 km away, on the other side of a bay, isn't their station.

ClimateExplorer shows each station's own record. It doesn't combine stations into a regional or global average, so a city station can't raise the numbers anywhere else. Most of the city stations are airports on the edge of town, not in the centre: the median is 5.8 km from the city centre.

If we ever do combine stations into an average, the city list is more likely to be used the other way round: to leave out stations near large cities, so that urban heat doesn't affect the result. That's what the major global temperature records do, in one form or another. They find that urban heat makes little difference to global averages, and the oceans, which have no cities, are warming too.

The full details of the city rule are in the [design notes](https://github.com/ledpup/ClimateExplorer/blob/master/docs/design/2026-11-18-01-city-stations-plan.md).
