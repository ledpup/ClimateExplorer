export function initialize(divId, options) {
    const newMap = L.map(divId, options).setView(options.center, options.zoom);
    return newMap;
}