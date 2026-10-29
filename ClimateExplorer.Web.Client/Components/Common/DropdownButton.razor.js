// Positions a DropdownButton's popover menu below its button while it's open. The browser does the rest
// (open/close, Escape, click outside, focus return).

import { place, autoUpdate } from "../../js/floating.js";

export function init(toggle, menu) {
    const options = { placement: menu.dataset.align === "end" ? "bottom-end" : "bottom-start", offset: 4 };
    let cleanup = null;

    const hide = () => {
        if (menu.matches(":popover-open")) {
            menu.hidePopover();
        }
    };

    // The toggle event fires after the menu is shown. The menu stays hidden (see app.css) until
    // data-placement is set, so it doesn't flash at the UA's default centred position.
    const onToggle = event => {
        cleanup?.();
        cleanup = null;

        if (event.newState === "open") {
            place(toggle, menu, options);
            cleanup = autoUpdate(toggle, menu, options);
        } else {
            delete menu.dataset.placement;
        }
    };

    // A link in the menu navigates without reloading the page, so close the menu ourselves
    const onClick = event => {
        if (event.target instanceof Element && event.target.closest("a[href]")) {
            hide();
        }
    };

    menu.addEventListener("toggle", onToggle);
    menu.addEventListener("click", onClick);

    return {
        hide,
        dispose() {
            menu.removeEventListener("toggle", onToggle);
            menu.removeEventListener("click", onClick);
            cleanup?.();
        },
    };
}
