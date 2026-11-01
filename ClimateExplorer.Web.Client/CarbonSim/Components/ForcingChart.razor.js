// The forcing by agent now, as horizontal bars. The last two bars are the total and the non-radiative heat, which sits
// below a divider because it isn't a forcing. Warming bars are red and cooling bars blue; the total is darker.
import "../../lib/chartjs/chart.umd.min.js";

const charts = new WeakMap();

const warming = "#d6604d";
const warmingTotal = "#b2182b";
const cooling = "#4393c3";
const coolingTotal = "#2166ac";
const notForcing = "#8c8c8c";

// Draws a line across the plot between the total and the non-radiative heat
const divider = {
    id: "forcingDivider",
    afterDatasetsDraw(chart) {
        const y = chart.scales.y;
        const count = chart.data.labels.length;
        if (count < 2) return;

        const at = (y.getPixelForValue(count - 2) + y.getPixelForValue(count - 1)) / 2;
        const { ctx, chartArea } = chart;

        ctx.save();
        ctx.strokeStyle = "rgb(0 0 0 / 35%)";
        ctx.setLineDash([4, 4]);
        ctx.beginPath();
        ctx.moveTo(0, at);
        ctx.lineTo(chartArea.right, at);
        ctx.stroke();
        ctx.restore();
    },
};

export function create(canvas, labels, values, xTitle, unit) {
    const chart = new Chart(canvas, {
        type: "bar",
        data: { labels, datasets: [{ data: values, backgroundColor: colours(values), barPercentage: 0.8 }] },
        options: {
            indexAxis: "y",
            animation: false,
            maintainAspectRatio: false,
            scales: {
                x: {
                    beginAtZero: true,
                    title: { display: true, text: xTitle },
                    ticks: { callback: (value) => trim(value) },
                    // Zero, where warming and cooling meet, is the darker line
                    grid: { color: (context) => (context.tick?.value === 0 ? "rgb(0 0 0 / 45%)" : "rgb(0 0 0 / 6%)") },
                },
                y: {
                    grid: { display: false },
                    ticks: { font: (context) => ({ weight: context.index === context.chart.data.labels.length - 2 ? "bold" : "normal" }) },
                },
            },
            plugins: {
                legend: { display: false },
                tooltip: { callbacks: { label: (item) => `${Number(item.parsed.x.toPrecision(3))} ${unit}` } },
            },
        },
        plugins: [divider],
    });

    charts.set(canvas, chart);
}

export function update(canvas, labels, values) {
    const chart = charts.get(canvas);
    if (!chart) return;

    chart.data.labels = labels;
    chart.data.datasets[0].data = values;
    chart.data.datasets[0].backgroundColor = colours(values);
    chart.update("none");
}

export function destroy(canvas) {
    charts.get(canvas)?.destroy();
    charts.delete(canvas);
}

function colours(values) {
    const total = values.length - 2;

    return values.map((value, i) => {
        if (i > total) return notForcing;
        if (i === total) return value < 0 ? coolingTotal : warmingTotal;
        return value < 0 ? cooling : warming;
    });
}

// Tick values can carry float noise, such as 0.30000000000000004
function trim(value) {
    return Number(value.toPrecision(10));
}
