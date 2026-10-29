// Positions a floating element (tooltip, dropdown menu, autocomplete list) next to an anchor element.
// The floating element is position: fixed, so it should be a top-layer popover or a child of <body>.

const opposite = { top: "bottom", bottom: "top", left: "right", right: "left" };

// "start"/"end" are the inline start/end sides of the anchor: left/right, or right/left in RTL.
function physicalSide(side, anchor) {
    if (side !== "start" && side !== "end") {
        return side;
    }

    const isRtl = getComputedStyle(anchor).direction === "rtl";
    return (side === "start") !== isRtl ? "left" : "right";
}

function clamp(value, min, max) {
    // When the floating element is bigger than the viewport, keep its start edge visible.
    return Math.max(min, Math.min(value, max));
}

// Where the floating element starts along the cross axis: centred on the anchor, or lined up with
// its start/end edge.
function crossAxisStart(anchorStart, anchorSize, floatingSize, align) {
    if (align === "start") return anchorStart;
    if (align === "end") return anchorStart + anchorSize - floatingSize;
    return anchorStart + (anchorSize - floatingSize) / 2;
}

// placement: "top" | "bottom" | "start" | "end" (or "left" | "right"), optionally with "-start"/"-end"
// alignment, e.g. "bottom-start". Flips to the opposite side if the preferred side lacks room, then
// shifts to stay `padding` px inside the viewport. Sets data-placement to the final side.
export function place(anchor, floating, { placement = "bottom", offset = 6, padding = 8 } = {}) {
    if (!anchor.isConnected || !floating.isConnected) {
        return;
    }

    const [preferredSide, align = "center"] = placement.split("-");
    let side = physicalSide(preferredSide, anchor);

    // Measure at the top-left so the viewport's right edge doesn't squeeze its fit-content width.
    Object.assign(floating.style, { position: "fixed", inset: "auto", left: "0px", top: "0px" });

    const a = anchor.getBoundingClientRect();
    const f = floating.getBoundingClientRect();
    const viewportWidth = document.documentElement.clientWidth;
    const viewportHeight = document.documentElement.clientHeight;

    const room = {
        top: a.top - padding,
        bottom: viewportHeight - a.bottom - padding,
        left: a.left - padding,
        right: viewportWidth - a.right - padding,
    };
    const isVertical = side === "top" || side === "bottom";
    const needed = (isVertical ? f.height : f.width) + offset;

    if (room[side] < needed && room[opposite[side]] > room[side]) {
        side = opposite[side];
    }

    let x;
    let y;
    if (side === "top" || side === "bottom") {
        x = crossAxisStart(a.left, a.width, f.width, align);
        y = side === "top" ? a.top - offset - f.height : a.bottom + offset;
    } else {
        x = side === "left" ? a.left - offset - f.width : a.right + offset;
        y = crossAxisStart(a.top, a.height, f.height, align);
    }

    x = clamp(x, padding, viewportWidth - f.width - padding);
    y = clamp(y, padding, viewportHeight - f.height - padding);

    floating.style.left = `${Math.round(x)}px`;
    floating.style.top = `${Math.round(y)}px`;
    floating.dataset.placement = side;
}

// Re-places the floating element on scroll (any scroll container) and resize while it's open.
// Returns a cleanup function to call when it closes.
export function autoUpdate(anchor, floating, options) {
    let frame = 0;
    const update = () => {
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(() => place(anchor, floating, options));
    };

    window.addEventListener("scroll", update, { capture: true, passive: true });
    window.addEventListener("resize", update, { passive: true });

    return () => {
        cancelAnimationFrame(frame);
        window.removeEventListener("scroll", update, { capture: true });
        window.removeEventListener("resize", update);
    };
}
