---
layout: single
title: "The one number"
date: 2026-10-01 09:00:00 +1000
categories: site-info
---

In the top-right corner of every page on ClimateExplorer there's a number. It isn't a visitor count or a version number: it's the amount of carbon dioxide (CO₂) in Earth's atmosphere. It's measured in parts per million (ppm), at the [Mauna Loa Observatory](https://en.wikipedia.org/wiki/Mauna_Loa_Observatory) in Hawaii. Hover over it for the details, and click it to open the recent CO₂ observations panel.

![The CO₂ number in the top-right corner of ClimateExplorer, with its tooltip: 429.51 parts per million, recorded August 2026 at Mauna Loa Observatory]({{site.url}}/blog/assets/co2-430ppm.png)

When hit by infrared light, the carbon dioxide molecule vibrates and heats the atmosphere. CO₂ is the main cause of recent global heating.

## Where the idea came from

On 1 November 2021, [David Attenborough](https://en.wikipedia.org/wiki/David_Attenborough) addressed world leaders at the opening of [COP26](https://en.wikipedia.org/wiki/2021_United_Nations_Climate_Change_Conference), the United Nations climate conference in Glasgow. Attenborough began:

> It's easy to forget that ultimately the climate emergency comes down to a single number — the concentration of carbon in our atmosphere.

He went on to say that for much of humanity's history, that number bounced between 180 and 300 parts per million. As he spoke, the screen behind him showed where it stood that day: 414.

Watch the address: [David Attenborough at COP26](https://www.youtube.com/watch?v=o7EpiXViSIQ).

The CO₂ concentration is the one number that sums up our predicament. (A full transcript of the speech is available from the [ABC](https://www.abc.net.au/news/2021-11-02/david-attenborough-speech-at-cop26-glasgow/100586992).)

## 417, 429, 430

The number we show is *deseasonalised*. Around the time of Attenborough's address, the deseasonalised figure was about **417 ppm**. When the number was added to the site in July 2026, it read **429**. This month it ticked over to **430** (the latest value, 429.51 for August 2026, rounds up to 430).

That's an increase of about **2.6 ppm per year** since Attenborough stood up in Glasgow and asked the world to act.

## What "deseasonalised" means

If you look at the raw monthly CO₂ measurements from Mauna Loa, they don't rise smoothly. They go up and down every year like a sawtooth. The cause is the plant life of the Northern Hemisphere, which holds most of the planet's land and forests. Each spring and summer, plants draw CO₂ out of the air as they grow, and the concentration falls. Each autumn and winter, leaves drop and decay, releasing CO₂ again, and the concentration rises. At Mauna Loa the peak comes around May and the low point around September/October, a swing of about 6 ppm each year.

That seasonal swing gets in the way when we want to study a much stronger signal: humans burning fossil fuels (coal, oil, and gas). The 2026 figures show the problem:

| Month (2026) | Monthly average | Deseasonalised |
|---|---|---|
| May | 432.34 | 429.06 |
| June | 431.43 | 428.99 |
| July | 429.13 | 428.78 |
| August | 427.55 | 429.51 |

Going by the raw monthly average, CO₂ fell by nearly 5 ppm between May and August. However, during this period (the Northern Hemisphere summer), plants were drawing CO₂ out of the atmosphere and into new growth as part of the annual cycle of life. The deseasonalised column removes that yearly cycle and leaves the underlying level.

This is also why Attenborough's screen said 414 while we quote 417 for 2021. The monthly average for October 2021 (near the seasonal low point) was 413.90 ppm. The deseasonalised value for that month was 417.14 ppm.

## How it's calculated

The numbers come from NOAA's Global Monitoring Laboratory, which publishes the [Mauna Loa monthly mean data](https://gml.noaa.gov/webdata/ccgg/trends/co2/co2_mm_mlo.txt) with a "monthly average" column and a "de-seasonalized" column side by side. The [NOAA Trends in CO₂ page](https://gml.noaa.gov/ccgg/trends/) describes the method:

> The black lines and symbols represent the same, after correction for the average seasonal cycle. The latter is determined as a moving average of SEVEN adjacent seasonal cycles centered on the month to be corrected, except for the first and last THREE and one-half years of the record, where the seasonal cycle has been averaged over the first and last SEVEN years, respectively.

![Recent monthly average Mauna Loa CO₂ until August 2026]({{site.url}}/blog/assets/co2_trend_mlo_2026.png)

In plain terms:

1. **Find the seasonal cycle.** For each year, work out how far each month sits above or below that year's underlying level. May is typically a few ppm above, and September a few ppm below.
2. **Average it over seven years.** A single year's cycle can be noisy, so NOAA averages the seasonal cycle across seven adjacent years, centred on the month being corrected: that year plus three either side.
3. **Subtract it.** Take the month's measured average and remove that month's average seasonal offset. What's left is the deseasonalised value.

The seasonal cycle for the most recent months can't be centred (the future years don't exist yet), so NOAA uses the last seven years instead. As new data arrives, the most recent deseasonalised values can shift by a small amount.

The deseasonalised value is still a single month's measurement, not a long-term average. It keeps the month-to-month noise, but removes the predictable annual swing.

## Mauna Loa Observatory

![Mauna Loa Observatory, above the clouds on the slopes of Mauna Loa, Hawaii. Photograph by Jonathan Kingston / National Geographic]({{site.url}}/blog/assets/mauna-loa-observatory.jpg)

*Photograph by Jonathan Kingston / National Geographic, from [National Geographic Education](https://education.nationalgeographic.org/resource/mauna-loa-observatory/).*

The [Mauna Loa Observatory](https://en.wikipedia.org/wiki/Mauna_Loa_Observatory) sits about 3,400 metres up the side of the Mauna Loa volcano on Hawaii's Big Island, far from major sources of pollution, in some of the cleanest air on Earth. Charles Keeling began measuring CO₂ there in 1958, and the site now holds the longest continuous direct record of atmospheric CO₂ in the world. From it, Keeling created the [Keeling Curve](https://keelingcurve.ucsd.edu/).

NOAA notes that because Mauna Loa is a single high-altitude site in the northern subtropics, its values may not be exactly the same as the global average CO₂ concentration at the surface. They're very close, though, and Mauna Loa's long, unbroken record is what makes it the reference.

### Interruptions

That record almost broke. In November 2022, Mauna Loa erupted and lava [buried the observatory's access road and power lines](https://www.noaa.gov/news/access-to-noaas-world-renowned-mauna-loa-observatory-restored). Within ten days NOAA staff had set up temporary CO₂ measurements at nearby Maunakea, and the record carried on. (The NOAA data file notes that observations from December 2022 to July 2023 came from Maunakea.)

In March 2025, the US government [listed the lease on NOAA's Hilo office for cancellation](https://www.hawaiitribune-herald.com/2025/03/11/hawaii-news/trump-cuts-target-world-leading-greenhouse-gas-observatory-in-hawaii/). [Forbes wrote](https://www.forbes.com/sites/we-dont-have-time/2026/05/30/mauna-loa-observatory-survives-lava-budget-cuts-and-politics/):

> As part of a broad cost-cutting drive, the Trump administration moved to cancel the lease on the federal office behind NOAA's Mauna Loa Observatory in Hawaii, home of the Keeling Curve and one of the most consequential scientific records humanity has ever produced. This spring [2026], NOAA announced the opposite: the same site was being reopened, rebuilt and expanded.

A temporary road through the lava was opened on 26 March 2026. NOAA is [redeveloping the site](https://www.noaa.gov/news/having-dodged-lava-flows-noaas-mauna-loa-research-facility-to-get-upgrades), with a new sampling tower, a renovated Keeling Building, more laboratory space and solar power so the observatory can keep running if the grid is cut again.

## Watching the number

The number in the corner updates each month. The [net-zero](https://en.wikipedia.org/wiki/Net-zero_emissions) goal, if it were achieved, would eventually stop the number from increasing. Most of the world's governments are now scaling back or abandoning their net-zero targets.

The burning of fossil fuels increases CO₂ in the atmosphere. The amount of CO₂ in our atmosphere is growing faster than at any time in human history. It is increasing at a rate that is at least 10 times faster than any other period we know of. It is the one number that represents the modern age.