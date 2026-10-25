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

const plugins = [axisTitleSwatches, cornerNote];

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
    new Chart(canvas, { type, data: { labels: [], datasets: [] }, options: {}, plugins });
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
        chart = new Chart(canvas, { type, data: { labels: [], datasets: [] }, options: {}, plugins });
    }

    chart.data.labels = labels;
    chart.data.datasets = datasets;
}

export function setOptions(canvas, options) {
    const chart = Chart.getChart(canvas);
    if (!chart) return;

    chart.options = options;
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
