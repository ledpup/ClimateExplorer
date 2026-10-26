// Both are UMD builds: importing them for their side effects defines the global Chart (which the
// tooltip and segment helpers in App.razor rely on) and registers the trendline plugin with it.
import "../../lib/chartjs/chart.umd.min.js";
import "../../lib/chartjs/chartjs-plugin-trendline.min.js";

const states = new WeakMap();

// Draws a filled square for each series colour at the start of every y axis title that has swatchColours,
// so the title text can stay black. Chart.js can't colour part of a title and doesn't expose its position.
const axisTitleSwatches = {
    id: "axisTitleSwatches",
    afterDraw(chart) {
        const ctx = chart.ctx;

        for (const scale of Object.values(chart.scales)) {
            const title = scale.options.title;
            const colours = title?.swatchColours;
            if (!title?.display || !colours?.length || !title.text || scale.isHorizontal()) continue;

            const font = Chart.helpers.toFont(title.font);
            const padding = Chart.helpers.toPadding(title.padding);
            const isLeft = scale.options.position === "left";
            const size = font.size * 1.05;
            const gap = 4;
            const textGap = 6;

            ctx.save();
            ctx.font = font.string;
            const textWidth = ctx.measureText(title.text).width;

            // A left axis title reads bottom to top, a right axis title top to bottom.
            // Nudged by a couple of pixels towards the top of the text so the square's bottom is flush with the baseline.
            const baselineNudge = 3;
            const centreX = isLeft
                ? scale.left + font.lineHeight / 2 + padding.top - baselineNudge
                : scale.right - font.lineHeight / 2 - padding.top + baselineNudge;
            const centreY = (scale.top + scale.bottom) / 2;

            // The first colour is read first, so it is the furthest from the text.
            colours.forEach((colour, i) => {
                const distance = (colours.length - 1 - i) * (size + gap);
                const y = isLeft
                    ? centreY + textWidth / 2 + textGap + distance
                    : centreY - textWidth / 2 - textGap - size - distance;

                ctx.fillStyle = colour;
                ctx.fillRect(centreX - size / 2, y, size, size);
            });

            ctx.restore();
        }
    }
};

// Draws options.plugins.cornerNote.text right-aligned on the subtitle's row, in the chart's top-right corner.
// Chart.js has only the title and subtitle, and each draws a single aligned block of text.
const cornerNote = {
    id: "cornerNote",
    afterDraw(chart, args, options) {
        if (!options?.text) return;

        // The subtitle isn't exposed like chart.titleBlock, so find it among the top layout boxes.
        const row = chart.boxes.find(b => b !== chart.titleBlock && b.position === "top" && b.options?.display && b.options.text)
            ?? chart.titleBlock;
        if (!row) return;

        const font = Chart.helpers.toFont(row.options.font);
        const padding = Chart.helpers.toPadding(row.options.padding);
        const ctx = chart.ctx;

        ctx.save();
        ctx.font = font.string;
        ctx.fillStyle = options.color ?? "black";
        ctx.textAlign = "right";
        ctx.textBaseline = "middle";
        ctx.fillText(options.text, row.right, row.top + padding.top + font.lineHeight / 2);
        ctx.restore();
    }
};

// The space kept between the series note and the nearest legend item.
const seriesNoteGap = 16;

// Draws options.plugins.seriesNote.text, the label parts every series shares, at the bottom left beside the legend.
// The legend lays its rows out across the whole width and centres each one, so the note's space is reserved by
// making the rows wrap at the width less the note on both sides: centred rows then stay clear of it, and the legend
// stays centred under the chart. If that would leave the legend less than half the width, the note gets its own row
// under the legend items instead.
//
// This wraps Legend._fitRows, which is private to Chart.js (unchanged from 4.5.0 to 4.6.1). Chart.js has no public way
// to do it: legend.maxWidth clamps the box but not the width the rows wrap at. Check it when upgrading Chart.js.
const seriesNote = {
    id: "seriesNote",
    beforeLayout(chart, args, options) {
        const legend = chart.legend;
        if (!legend) return;

        if (!legend.seriesNoteFitRows) {
            legend.seriesNoteFitRows = legend._fitRows;
            legend._fitRows = fitLegendRowsAroundSeriesNote;
        }

        legend.seriesNoteText = options?.text || null;
    },
    afterDraw(chart, args, options) {
        const legend = chart.legend;
        const boxes = legend?.legendHitBoxes;
        if (!options?.text || !legend.options.display || !boxes?.length) return;

        // The legend draws each item's text vertically centred in its hit box; the last box is on the last row.
        const last = boxes[boxes.length - 1];
        const rowStep = legend.seriesNoteOwnRow ? last.height + legend.options.labels.padding : 0;
        const ctx = chart.ctx;

        ctx.save();
        ctx.font = seriesNoteFont(legend);
        ctx.fillStyle = options.color ?? "black";
        ctx.textAlign = "left";
        ctx.textBaseline = "middle";
        ctx.fillText(options.text, legend.left + legend.options.labels.padding, last.top + last.height / 2 + rowStep);
        ctx.restore();
    }
};

// The note is bold so it reads as a heading for the whole legend rather than another series.
function seriesNoteFont(legend) {
    return Chart.helpers.toFont({ ...legend.options.labels.font, weight: "bold" }).string;
}

// Replaces legend._fitRows (called with the legend as this). Legend.fit has already set ctx.font to the label font,
// and reads the full maxWidth before calling it, so the legend box keeps the full width.
function fitLegendRowsAroundSeriesNote(titleHeight, fontSize, boxWidth, itemHeight) {
    const fitRows = this.seriesNoteFitRows;
    this.seriesNoteOwnRow = false;

    if (!this.seriesNoteText) return fitRows.apply(this, arguments);

    const fullWidth = this.maxWidth;
    const labelFont = this.ctx.font;
    this.ctx.font = seriesNoteFont(this);
    const reserved = 2 * (this.ctx.measureText(this.seriesNoteText).width + seriesNoteGap);
    this.ctx.font = labelFont;

    if (fullWidth - reserved >= fullWidth / 2) {
        this.maxWidth = fullWidth - reserved;
        try {
            return fitRows.apply(this, arguments);
        } finally {
            this.maxWidth = fullWidth;
        }
    }

    this.seriesNoteOwnRow = true;
    return fitRows.apply(this, arguments) + itemHeight + this.options.labels.padding;
}

const plugins = [axisTitleSwatches, cornerNote, seriesNote];

// Chart.js sizes the canvas when the chart is constructed, and again only when its container resizes (or resize() is
// called), not on update. Its defaults keep a 2:1 aspect ratio, so a chart constructed or resized without these would
// be sized at the full width and half that high (overflowing the wrapper), or capped at the wrapper's height and only
// twice that wide, and would keep that size after the real options arrive.
const sizingOptions = { responsive: true, maintainAspectRatio: false };

export function create(canvas, type, dotNetRef) {
    destroy(canvas);

    const state = { resizeRequestId: null };

    // Chart.js resizes itself when its container does, but a canvas left with the inline width and
    // height of a wider window can stop the container shrinking. Clear them on window resize.
    state.onWindowResize = () => scheduleResize(canvas, state);
    window.addEventListener("resize", state.onWindowResize);

    canvas.onclick = event => {
        const chart = Chart.getChart(canvas);
        const points = chart?.getElementsAtEventForMode(event, "nearest", { intersect: true }, false);

        if (points?.length > 0) {
            dotNetRef.invokeMethodAsync("OnChartClicked", points[0].datasetIndex, points[0].index);
        }
    };

    states.set(canvas, state);
    new Chart(canvas, { type, data: { labels: [], datasets: [] }, options: { ...sizingOptions }, plugins });
}

export function clear(canvas) {
    const chart = Chart.getChart(canvas);
    if (!chart) return;

    chart.data.labels = [];
    chart.data.datasets = [];
    chart.update();
}

// The chart-level type only changes when a bar series is added or removed, so recreating the chart
// for it is fine. Options are replaced by setOptions straight after.
export function setData(canvas, type, labels, datasets) {
    let chart = Chart.getChart(canvas);
    if (!chart) return;

    if (chart.config.type !== type) {
        chart.destroy();
        chart = new Chart(canvas, { type, data: { labels: [], datasets: [] }, options: { ...sizingOptions }, plugins });
    }

    chart.data.labels = labels;
    chart.data.datasets = datasets;
}

export function setOptions(canvas, options) {
    const chart = Chart.getChart(canvas);
    if (!chart) return;

    // ChartView passes {} when there is nothing to draw, which would otherwise bring back the aspect ratio.
    chart.options = { ...sizingOptions, ...options };
}

export function update(canvas) {
    Chart.getChart(canvas)?.update();
}

export function resize(canvas) {
    const state = states.get(canvas);
    if (state) scheduleResize(canvas, state);
}

export function destroy(canvas) {
    const state = states.get(canvas);

    if (state) {
        window.removeEventListener("resize", state.onWindowResize);

        if (state.resizeRequestId) {
            cancelAnimationFrame(state.resizeRequestId);
        }

        states.delete(canvas);
    }

    canvas.onclick = null;
    Chart.getChart(canvas)?.destroy();
}

function scheduleResize(canvas, state) {
    if (state.resizeRequestId) return;

    state.resizeRequestId = requestAnimationFrame(() => {
        state.resizeRequestId = null;

        canvas.style.removeProperty("width");
        canvas.style.removeProperty("height");
        Chart.getChart(canvas)?.resize();
    });
}
