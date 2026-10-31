// The gases over time, as a line per gas. Points are appended as the run goes; past maxPoints every other point is
// dropped, so a long run keeps its shape without slowing down.
import "../../lib/chartjs/chart.umd.min.js";

const charts = new WeakMap();
const maxPoints = 1500;

// Julian year, as CarbonSimFormat.Duration
const year = 365.25 * 86400;

export function create(canvas, series, yTitle) {
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
                    beginAtZero: true,
                    title: { display: true, text: yTitle },
                    ticks: { callback: (value) => `${value}%` },
                    grid: { color: "rgb(0 0 0 / 6%)" },
                },
            },
            plugins: {
                legend: { position: "bottom", labels: { usePointStyle: true, pointStyle: "line" } },
                tooltip: {
                    callbacks: {
                        title: (items) => (items.length ? formatTime(items[0].parsed.x) : ""),
                        label: (item) => `${item.dataset.label}: ${formatPercent(item.parsed.y)}`,
                    },
                },
            },
        },
    });

    charts.set(canvas, chart);
}

export function reset(canvas, series) {
    const chart = charts.get(canvas);
    if (!chart) return;

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

export function destroy(canvas) {
    charts.get(canvas)?.destroy();
    charts.delete(canvas);
}

function toDatasets(series) {
    return series.map((s) => ({
        label: s.label,
        borderColor: s.colour,
        backgroundColor: s.colour,
        hidden: s.hidden,
        data: [],
    }));
}

function formatPercent(value) {
    return `${Number(value.toPrecision(3))}%`;
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
