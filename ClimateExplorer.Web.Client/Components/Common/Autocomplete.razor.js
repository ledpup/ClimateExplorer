// Blazor can't cancel a key's default action per key, so this does it for the keys the combobox handles: the arrows
// (which would move the caret), Enter while an option is active, and Escape while the list is open. Cancelling that
// Escape keydown stops it also closing a <dialog> the input is in, so a second Escape is needed for that.
export function init(input, list) {
    const onKeyDown = event => {
        const expanded = input.getAttribute("aria-expanded") === "true";

        if (event.key === "ArrowDown"
            || event.key === "ArrowUp"
            || (event.key === "Escape" && expanded)
            || (event.key === "Enter" && expanded && input.hasAttribute("aria-activedescendant"))) {
            event.preventDefault();
        }
    };

    input.addEventListener("keydown", onKeyDown);

    return {
        scrollActiveIntoView() {
            list.querySelector(".is-active")?.scrollIntoView({ block: "nearest" });
        },
        dispose() {
            input.removeEventListener("keydown", onKeyDown);
        },
    };
}
