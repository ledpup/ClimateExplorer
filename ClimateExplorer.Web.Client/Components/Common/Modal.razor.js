// Opens and closes a Modal's <dialog>. showModal() does the rest: the top layer, an inert page behind (focus
// stays in the dialog), Escape to close, and focus returning to the opener. This adds closing on a backdrop click.

// The page can't scroll behind an open modal (see app.css), so its scrollbar goes. Measure it first, so app.css can
// pad the page by the same width.
function measureScrollbar() {
    if (!document.querySelector("dialog.modal[open]")) {
        const root = document.documentElement;
        root.style.setProperty("--climate-modal-scrollbar-width", `${window.innerWidth - root.clientWidth}px`);
    }
}

export function init(dialog) {
    // A press that starts in the content and is released over the backdrop (selecting text, say) isn't a
    // backdrop click, so remember where it started
    let pressedBackdrop = false;

    const onPointerDown = event => {
        pressedBackdrop = event.target === dialog;
    };

    // The dialog is transparent around the visible box, so a click on the dialog itself is a click on the backdrop
    const onClick = event => {
        if (pressedBackdrop && event.target === dialog) {
            dialog.close();
        }

        pressedBackdrop = false;
    };

    dialog.addEventListener("pointerdown", onPointerDown);
    dialog.addEventListener("click", onClick);

    return {
        show() {
            if (!dialog.open) {
                measureScrollbar();
                dialog.showModal();
            }
        },
        close() {
            if (dialog.open) {
                dialog.close();
            }
        },
        dispose() {
            dialog.removeEventListener("pointerdown", onPointerDown);
            dialog.removeEventListener("click", onClick);

            if (dialog.open) {
                dialog.close();
            }
        },
    };
}
