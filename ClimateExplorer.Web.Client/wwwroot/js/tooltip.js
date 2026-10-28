// Shows one shared tooltip for every [data-tooltip] element (rendered by DelayedTooltip), using
// event delegation on the document. Loaded once from App.razor, so it works during prerender and
// needs no per-component interop. The tooltip is a manual popover, so it renders in the top layer:
// never clipped by overflow: hidden ancestors, and above an open <dialog>.

import { place } from "./floating.js";

const tooltipId = "climate-tooltip";
const showDelay = 500;
const hideDelay = 500;

// Tooltip text is our own markup (<p>, <br>, <b>, a table of trend rates). Anything else is reduced to
// its text, and every attribute except style is dropped.
const allowedTags = new Set(["P", "BR", "B", "STRONG", "I", "EM", "SPAN", "DIV", "UL", "OL", "LI", "TABLE", "THEAD", "TBODY", "TR", "TH", "TD"]);

let tip = null;
let anchor = null;          // the anchor the pointer is over, or that has keyboard focus
let visible = false;
let suppressedAnchor = null; // pressed while hovered: stays hidden until the pointer leaves it
let focusElement = null;     // the focused control, while shown from keyboard focus
let describedElement = null;
let showTimer = 0;
let hideTimer = 0;

function getTip() {
    // Enhanced navigation can replace <body>'s children, so check it's still in the document.
    if (!tip?.isConnected) {
        tip = document.getElementById(tooltipId) ?? document.createElement("div");
        tip.id = tooltipId;
        tip.className = "climate-tooltip";
        tip.setAttribute("role", "tooltip");
        tip.setAttribute("popover", "manual");
        document.body.appendChild(tip);
    }

    return tip;
}

function sanitize(node) {
    for (const child of [...node.childNodes]) {
        if (child.nodeType === Node.TEXT_NODE) {
            continue;
        }

        if (child.nodeType !== Node.ELEMENT_NODE) {
            child.remove();
        } else if (!allowedTags.has(child.tagName)) {
            child.replaceWith(document.createTextNode(child.textContent));
        } else {
            for (const name of child.getAttributeNames()) {
                if (name !== "style") {
                    child.removeAttribute(name);
                }
            }

            sanitize(child);
        }
    }
}

function toContent(html) {
    // Template content is inert: nothing in it loads or runs while it's sanitised.
    const template = document.createElement("template");
    template.innerHTML = html;
    sanitize(template.content);
    return template.content;
}

function clearTimers() {
    clearTimeout(showTimer);
    clearTimeout(hideTimer);
}

// focusTarget: the focused control when shown from keyboard focus. describe: make the tooltip its
// accessible description.
function show(target, focusTarget, describe) {
    clearTimers();
    const text = target.getAttribute("data-tooltip");
    if (!target.isConnected || !text) {
        hide();
        return;
    }

    const element = getTip();
    element.replaceChildren(toContent(text));
    element.classList.toggle("is-multiline", target.hasAttribute("data-tooltip-multiline"));

    if (!element.matches(":popover-open")) {
        element.showPopover?.();
    }

    place(target, element, { placement: target.getAttribute("data-tooltip-placement") ?? "top" });
    anchor = target;
    visible = true;
    focusElement = focusTarget;

    setDescribedElement(describe ? focusTarget : null);
}

function hide() {
    clearTimers();
    anchor = null;
    focusElement = null;
    setDescribedElement(null);

    if (!visible) {
        return;
    }

    visible = false;
    if (tip?.matches(":popover-open")) {
        tip.hidePopover();
    }
}

// While shown from keyboard focus, the tooltip is the focused control's description, read after
// its own name.
function setDescribedElement(element) {
    if (describedElement && describedElement !== element) {
        const ids = (describedElement.getAttribute("aria-describedby") ?? "").split(" ").filter(x => x && x !== tooltipId);
        if (ids.length > 0) {
            describedElement.setAttribute("aria-describedby", ids.join(" "));
        } else {
            describedElement.removeAttribute("aria-describedby");
        }
    }

    describedElement = element;

    if (element) {
        const ids = (element.getAttribute("aria-describedby") ?? "").split(" ").filter(Boolean);
        if (!ids.includes(tooltipId)) {
            element.setAttribute("aria-describedby", [...ids, tooltipId].join(" "));
        }
    }
}

function anchorOf(target) {
    return target instanceof Element ? target.closest("[data-tooltip]") : null;
}

document.addEventListener("pointerover", event => {
    // Touch never shows tooltips, so a tap can't leave one stuck open.
    if (event.pointerType === "touch") {
        return;
    }

    const target = anchorOf(event.target);
    if (!target || target === suppressedAnchor) {
        return;
    }

    if (target === anchor) {
        clearTimeout(hideTimer);
        return;
    }

    clearTimers();

    if (visible) {
        // Moving straight to another anchor switches without a second delay.
        show(target, null, false);
        return;
    }

    anchor = target;
    showTimer = setTimeout(() => show(target, null, false), showDelay);
});

document.addEventListener("pointerout", event => {
    const target = anchorOf(event.target);
    if (!target || target.contains(event.relatedTarget)) {
        return;
    }

    if (target === suppressedAnchor) {
        suppressedAnchor = null;
    }

    if (target !== anchor || focusElement) {
        return;
    }

    clearTimeout(showTimer);

    if (visible) {
        hideTimer = setTimeout(hide, hideDelay);
    } else {
        anchor = null;
    }
});

document.addEventListener("focusin", event => {
    const target = anchorOf(event.target);
    if (target && event.target.matches(":focus-visible")) {
        // Many controls' names are the same as their tooltip; don't have that read twice.
        const isOwnName = event.target.getAttribute("aria-label") === target.getAttribute("data-tooltip");
        show(target, event.target, !isOwnName);
    }
});

document.addEventListener("focusout", event => {
    if (event.target === focusElement) {
        hide();
    }
});

document.addEventListener("pointerdown", () => {
    suppressedAnchor = anchor;
    hide();
});

document.addEventListener("keydown", event => {
    if (event.key === "Escape" && visible) {
        suppressedAnchor = focusElement ? null : anchor;
        hide();
    }
});

window.addEventListener("scroll", () => {
    if (anchor) {
        hide();
    }
}, { capture: true, passive: true });

window.addEventListener("resize", hide, { passive: true });
