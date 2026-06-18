(function () {
    const prefix = "ClimateExplorer";

    window.climateExplorerPerformance = {
        mark: function (name, detail) {
            if (!window.performance || !window.performance.mark) return;
            performance.mark(`${prefix}:${name}`, { detail: detail || {} });
            console.debug(`${prefix}:mark`, name, detail || {});
        },
        measure: function (name, startMark, endMark, detail) {
            if (!window.performance || !window.performance.measure) return;
            const measureName = `${prefix}:${name}`;
            const start = `${prefix}:${startMark}`;
            const end = endMark ? `${prefix}:${endMark}` : undefined;
            try {
                performance.measure(measureName, { start: start, end: end, detail: detail || {} });
                const entries = performance.getEntriesByName(measureName);
                const latest = entries[entries.length - 1];
                console.info(`${prefix}:measure`, name, latest ? Math.round(latest.duration) : null, detail || {});
            } catch (error) {
                console.debug(`${prefix}:measure skipped`, name, error);
            }
        }
    };

    window.climateExplorerPerformance.mark("document-script-loaded", { url: window.location.href });
})();
