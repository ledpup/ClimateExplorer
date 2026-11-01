// Values over time, as a line per series. Points are appended as the run goes; past maxPoints every other point is
// dropped, so a long run keeps its shape without slowing down.
import "../../lib/chartjs/chart.umd.min.js";

const charts = new WeakMap();
const maxPoints = 1500;

// Julian year, as CarbonSimFormat.Duration
const year = 365.25 * 86400;

export function create(canvas, series, yTitle, yUnit, beginAtZero) {
    // The callbacks run while the chart is being built, so they read the unit from here, not from the chart
    const units = { y: yUnit };

    const chart = new Chart(canvas, {
        type: "line",
        data: { datasets: toDatasets(series) },
        options: {
            animation: false,
            maintainAspectRatio: false,
            parsing: false,
            normalized: true,
            interaction: { mode: "index", intersect: false },
            elements: {
                point: { radius: 0, hoverRadius: 4 },
                line: { borderWidth: 2, tension: 0 },
            },
            scales: {
                x: {
                    type: "linear",
                    min: 0,
                    title: { display: true, text: "Time since the start" },
                    ticks: { callback: (value) => formatTime(value), maxTicksLimit: 8 },
                    grid: { color: "rgb(0 0 0 / 6%)" },
                },
                y: {
                    beginAtZero,
                    title: { display: true, text: yTitle },
                    ticks: { callback: (value) => `${trim(value)}${units.y}` },
                    grid: { color: "rgb(0 0 0 / 6%)" },
                },
            },
            plugins: {
                legend: { position: "bottom", labels: { usePointStyle: true, pointStyle: "line" } },
                tooltip: {
                    callbacks: {
                        title: (items) => (items.length ? formatTime(items[0].parsed.x) : ""),
                        label: (item) => `${item.dataset.label}: ${Number(item.parsed.y.toPrecision(3))}${units.y}`,
                    },
                },
            },
        },
    });

    chart.$units = units;
    charts.set(canvas, chart);
}

export function reset(canvas, series, yUnit) {
    const chart = charts.get(canvas);
    if (!chart) return;

    chart.$units.y = yUnit;
    chart.data.datasets = toDatasets(series);
    chart.update();
}

export function append(canvas, time, values) {
    const chart = charts.get(canvas);
    if (!chart) return;

    chart.data.datasets.forEach((dataset, i) => {
        dataset.data.push({ x: time, y: values[i] });

        if (dataset.data.length > maxPoints) {
            // Keep the first and last points so the line still starts and ends in the right place
            dataset.data = dataset.data.filter((_, j, all) => j % 2 === 0 || j === all.length - 1);
        }
    });

    chart.update("none");
}

export function convert(canvas, scale, offset, yUnit) {
    const chart = charts.get(canvas);
    if (!chart) return;

    chart.$units.y = yUnit;
    chart.data.datasets.forEach((dataset) => {
        dataset.data = dataset.data.map((point) => ({ x: point.x, y: point.y * scale + offset }));
    });

    chart.update("none");
}

export function destroy(canvas) {
    charts.get(canvas)?.destroy();
    charts.delete(canvas);
}

function toDatasets(series) {
    return series.map((s) => ({
        label: s.label,
        borderColor: s.colour,
        backgroundColor: s.colour,
        borderDash: s.dashed ? [6, 4] : [],
        hidden: s.hidden,
        data: [],
    }));
}

// Tick values can carry float noise, such as 0.30000000000000004
function trim(value) {
    return Number(value.toPrecision(10));
}

function formatTime(seconds) {
    if (seconds < 120) return `${round(seconds)} s`;
    if (seconds < 7200) return `${round(seconds / 60)} min`;
    if (seconds < 172800) return `${round(seconds / 3600)} h`;
    if (seconds < 2 * year) return `${round(seconds / 86400)} days`;
    return `${round(seconds / year)} years`;
}

function round(value) {
    return Number(value.toFixed(value < 10 ? 1 : 0));
}
