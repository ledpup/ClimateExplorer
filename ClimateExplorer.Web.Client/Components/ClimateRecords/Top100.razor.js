const states = new WeakMap();

// Reports the element's width to .NET whenever it changes, so the chart can be redrawn at its real
// pixel size. Scaling a fixed-size SVG to fit with a viewBox would scale its text along with it.
export function observeWidth(element, dotNetRef) {
    unobserveWidth(element);

    const state = { width: 0, requestId: null };

    state.observer = new ResizeObserver(() => {
        if (state.requestId) return;

        state.requestId = requestAnimationFrame(() => {
            state.requestId = null;

            const width = Math.floor(element.clientWidth);
            if (width <= 0 || width === state.width) return;

            state.width = width;
            dotNetRef.invokeMethodAsync("OnWidthChanged", width);
        });
    });

    state.observer.observe(element);
    states.set(element, state);
}

export function unobserveWidth(element) {
    const state = states.get(element);
    if (!state) return;

    state.observer.disconnect();
    if (state.requestId) {
        cancelAnimationFrame(state.requestId);
    }

    states.delete(element);
}
