// Generic, reusable scroll helper for pages that need to scroll to an element
// while accounting for a fixed/sticky header (e.g., the app navbar) that would
// otherwise cover the top of the target element.

export function scrollToElementBelowHeader(elementId, headerSelector = '.app-navbar', extraGapPx = 12) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    const header = document.querySelector(headerSelector);
    const headerHeight = header ? header.getBoundingClientRect().height : 0;

    const elementTop = element.getBoundingClientRect().top + window.scrollY;
    const targetY = elementTop - headerHeight - extraGapPx;

    window.scrollTo({ top: Math.max(targetY, 0), behavior: 'smooth' });
}
