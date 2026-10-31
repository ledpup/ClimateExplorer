// Drags a FloatingPanel by its header, and moves it with the arrow keys on its handle. The offset is a CSS translate,
// so the panel keeps its place in the layout. The panel last touched is brought to the front.
const states = new WeakMap();
const mobile = window.matchMedia("(max-width: 767px)");
const keyStep = 16;
const largeKeyStep = 64;
let front = 1;

export function init(panel, header) {
    const state = { x: 0, y: 0, drag: null };
    const handle = header.querySelector(".floating-panel-handle");

    const move = (x, y) => {
        state.x = x;
        state.y = y;
        panel.style.translate = x === 0 && y === 0 ? "" : `${x}px ${y}px`;
    };

    const raise = () => {
        panel.style.zIndex = String(++front);
    };

    state.onPointerDown = (e) => {
        if (mobile.matches || e.button !== 0) return;
        raise();
        header.setPointerCapture(e.pointerId);
        state.drag = { pointerId: e.pointerId, startX: e.clientX - state.x, startY: e.clientY - state.y };
        panel.classList.add("is-dragging");
        e.preventDefault();
    };

    state.onPointerMove = (e) => {
        if (state.drag?.pointerId !== e.pointerId) return;
        move(e.clientX - state.drag.startX, e.clientY - state.drag.startY);
    };

    state.onPointerUp = (e) => {
        if (state.drag?.pointerId !== e.pointerId) return;
        state.drag = null;
        panel.classList.remove("is-dragging");
    };

    state.onKeyDown = (e) => {
        if (mobile.matches) return;
        const step = e.shiftKey ? largeKeyStep : keyStep;
        const moves = {
            ArrowLeft: [-step, 0],
            ArrowRight: [step, 0],
            ArrowUp: [0, -step],
            ArrowDown: [0, step],
        };

        if (e.key === "Home") {
            move(0, 0);
        } else if (moves[e.key]) {
            raise();
            move(state.x + moves[e.key][0], state.y + moves[e.key][1]);
        } else {
            return;
        }

        e.preventDefault();
    };

    state.onFocusIn = raise;

    // On mobile the panels stack in the page, so drop any offset.
    state.onMediaChange = () => {
        if (mobile.matches) move(0, 0);
    };

    state.header = header;
    state.handle = handle;
    header.addEventListener("pointerdown", state.onPointerDown);
    header.addEventListener("pointermove", state.onPointerMove);
    header.addEventListener("pointerup", state.onPointerUp);
    header.addEventListener("pointercancel", state.onPointerUp);
    handle?.addEventListener("keydown", state.onKeyDown);
    panel.addEventListener("focusin", state.onFocusIn);
    mobile.addEventListener("change", state.onMediaChange);
    states.set(panel, state);
}

export function dispose(panel) {
    const state = states.get(panel);
    if (!state) return;

    state.header.removeEventListener("pointerdown", state.onPointerDown);
    state.header.removeEventListener("pointermove", state.onPointerMove);
    state.header.removeEventListener("pointerup", state.onPointerUp);
    state.header.removeEventListener("pointercancel", state.onPointerUp);
    state.handle?.removeEventListener("keydown", state.onKeyDown);
    panel.removeEventListener("focusin", state.onFocusIn);
    mobile.removeEventListener("change", state.onMediaChange);
    states.delete(panel);
}
