// The tab ids in document order. Tabs register with .NET in the order they're created, so a tab added later
// by an @if (between tabs that already exist) registers last but sits in the middle of the strip.
export function getTabIds(tablist) {
    return Array.from(tablist.querySelectorAll('[role="tab"]'), tab => tab.id);
}
