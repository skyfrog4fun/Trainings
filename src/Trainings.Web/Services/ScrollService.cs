using Microsoft.JSInterop;

namespace Trainings.Web.Services;

/// <summary>
/// Encapsulates JS interop needed to smoothly scroll the page to a named marker element
/// (an element carrying a matching id, e.g. rendered via the ScrollAnchor component),
/// accounting for the fixed app header. Pages inject this service instead of dealing with
/// JS module import/dispose directly. If the marker element does not exist in the DOM at
/// the time of the call, no scrolling occurs (no exception is thrown).
///
/// Usage - Case 1: target element already rendered (simple button click).
/// Use this when the marker is always present in the DOM (e.g. a static section further
/// down the page). Call ScrollToAsync directly from the event handler, e.g.:
///   onclick handler: goes to ScrollService.ScrollToAsync("group-access-section")
///   and place a ScrollAnchor with Id="group-access-section" where you want to land.
///
/// Usage - Case 2: target element appears conditionally as a result of the same click
/// (e.g. opening a form that is only rendered when a flag becomes true). At the moment the
/// event handler runs, Blazor has not yet re-rendered, so the marker element does not exist
/// in the DOM yet, and an immediate ScrollToAsync call would silently no-op. Instead, call
/// RequestScroll from the event handler to remember the intent, and call
/// ScrollToPendingAsync unconditionally from OnAfterRenderAsync, which runs after the DOM
/// has been updated:
///   ShowCreateForm(): set the show-form flag to true, then call
///     ScrollService.RequestScroll("location-edit-form");
///   OnAfterRenderAsync(bool firstRender): await ScrollService.ScrollToPendingAsync();
/// </summary>
public sealed class ScrollService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private IJSObjectReference? _jsModule;
    private string? _pendingMarkerId;

    /// <summary>
    /// Marks the given marker id to be scrolled to after the next render. Call this from an
    /// event handler when the target element is being rendered as a result of this same
    /// state change (e.g. showing a form), then call ScrollToPendingAsync from
    /// OnAfterRenderAsync. See remarks on ScrollService for a full example.
    /// </summary>
    public void RequestScroll(string markerId) => _pendingMarkerId = markerId;

    /// <summary>
    /// Scrolls to the marker requested via RequestScroll, if any, and clears the pending
    /// request. Safe to call unconditionally from OnAfterRenderAsync on every render; it is
    /// a no-op when nothing is pending.
    /// </summary>
    public async Task ScrollToPendingAsync()
    {
        if (_pendingMarkerId is null)
        {
            return;
        }

        var markerId = _pendingMarkerId;
        _pendingMarkerId = null;
        await ScrollToAsync(markerId);
    }

    /// <summary>
    /// Scrolls to the element with the given id immediately, if it already exists in the
    /// current page. Use this for simple cases where the target is always rendered (e.g. a
    /// "jump to section" button). See remarks on ScrollService for a full example.
    /// </summary>
    public async Task ScrollToAsync(string markerId)
    {
        _jsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/scrollHelper.js");
        await _jsModule.InvokeVoidAsync("scrollToElementBelowHeader", markerId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
