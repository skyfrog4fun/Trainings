// Measures whether an element's content is being clipped (its scrollable content is
// taller than its visible box), used to decide whether an ExpandableTextComponent
// needs to show its "Show more" toggle at all.

export function hasOverflow(element) {
    if (!element) {
        return false;
    }

    return element.scrollHeight > element.clientHeight + 1;
}
