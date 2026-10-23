// Both are UMD builds: importing them for their side effects defines the global Chart (which the
// tooltip and segment helpers in App.razor rely on) and registers the trendline plugin with it.
import "../../lib/chartjs/chart.umd.min.js";
import "../../lib/chartjs/chartjs-plugin-trendline.min.js";

const states = new WeakMap();

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
    new Chart(canvas, { type, data: { labels: [], datasets: [] }, options: {} });
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
        chart = new Chart(canvas, { type, data: { labels: [], datasets: [] }, options: {} });
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
