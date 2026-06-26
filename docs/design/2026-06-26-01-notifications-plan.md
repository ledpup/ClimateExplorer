# Notifications plan

- **Date:** 2026-06-26
- **Status:** Stages 1-2 implemented 2026-06-26; Stage 3 proposed
- **Author:** Patrick Lea (with Codex)
- **Scope:** `NavMenu`, `MainLayout`, notification UI components, chart/user-facing message models, chart page message handling, snackbar replacement, and session-scoped client notification state.
- **Branch context:** `development`

## Summary

ClimateExplorer currently uses Blazorise snackbars for a small set of chart-related user messages. The messages are useful, but the delivery mechanism is transient and page-local. The proposed notification feature should move these user-visible diagnostics into a session-scoped notification system with a nav bell, unread count, toast prompt, side panel, and a table of notifications.

The change should be staged. Stage 1 adds the notification entry point and unread behavior. Stage 2 adds the side panel and read-state handling. Stage 3 migrates snackbar messages into notifications and removes the snackbar dependency from this path.

No new notification UI should use Blazorise. Existing unrelated Blazorise controls can remain unless a later broader UI refactor decides otherwise.

## Current state

The current snackbar model is `ClimateExplorer.Web.Client/UiModel/SnackbarMessage.cs`. It depends directly on `Blazorise.Snackbar.SnackbarColor`.

The active snackbar presentation is owned by `ChartablePage`:

- `Index.razor` and `RegionalAndGlobal.razor` render `SnackbarStack`.
- `ChartablePage.SnackbarMessageEventHandler` deduplicates currently active messages by message text.
- `ChartablePage` pushes messages into the Blazorise snackbar stack for chart build failures, empty presets, chart data warnings, and location substitution warnings.

Known user-facing message producers include:

- `ChartDataBuilder` warnings for completeness filtering and moving-average fallback.
- `ChartSeriesLocationSubstitutionService` warnings when a data type is not available at the new location.
- `ChartablePage` catch blocks for chart build failures.
- `ChartablePage.OnChartPresetSelected` when a preset has no chart series.

There are many `ILogger` calls across the app, but most are operational diagnostics and should remain logs. A notification should be created only when a message is useful to the user in the current session.

## Existing UI building blocks

`NavMenu` currently uses Blazorise bar/dropdown components, but it can host plain HTML button components inside nav items. The notification bell should be implemented as a plain Blazor component and styled with CSS isolation/global CSS as appropriate.

The app already has a reusable plain `SidePanel` component in `ClimateExplorer.Web.Client/Components/Common/SidePanel.razor`. The notifications side panel should reuse this component instead of adding a new drawer dependency.

The app has global table styling in `ClimateExplorer.Web/wwwroot/css/table.css`, used by climate-record tables. The notifications table should reuse those classes and conventions.

## Design principles

- Notifications are session state, not durable user history.
- Notifications survive page navigation within the current app session.
- Notifications clear when the session ends or the page reloads.
- The bell is disabled only when there are no notifications at all.
- Read state affects unread count and bell colour, not whether the bell can open the panel.
- New notification UI does not use Blazorise.
- User-facing diagnostics become notifications; routine logs remain logs.
- Repeated identical notifications are grouped rather than spammed into the list.
- Motion must respect `prefers-reduced-motion`.

## Proposed data model

Introduce notification-specific models instead of extending `SnackbarMessage`.

```csharp
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success,
}

public sealed record UserNotification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Message { get; init; }
    public NotificationType Type { get; init; } = NotificationType.Info;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Count { get; set; } = 1;
    public Guid? LocationId { get; init; }
    public string? LocationName { get; init; }
    public string? ActionText { get; init; }
    public string? ActionUrl { get; init; }
}
```

Names are illustrative. The important parts are type, read state, timestamp, optional location context, optional action/link, and grouping count.

## Proposed service

Add a scoped notification service in the client project.

Responsibilities:

- hold the session notification list;
- expose unread count and total count;
- add notifications;
- group identical notifications;
- mark one/all notifications read;
- request side-panel opening;
- raise change events for subscribed UI components;
- raise a distinct event when unread count transitions from `0` to `1`.

Grouping key should include:

- normalized message;
- notification type;
- location id or location name;
- action URL.

When an existing notification is grouped:

- increment `Count`;
- update `UpdatedAt`;
- set `IsRead = false`;
- raise change notifications.

Unread count can count grouped occurrences rather than rows. For example, one grouped unread row with `Count = 3` contributes 3 to the unread badge. This matches the user's "identical notifications be grouped with a count" requirement while still making repeated events visible.

## Stage 1: Notification bell and unread count

Add a `NotificationBell` component and place it in `NavMenu`.

Placement:

- desktop: after the secondary nav items, as the last nav item;
- mobile: immediately before the hamburger menu item;
- do not put the bell inside the hamburger dropdown.

Behavior:

- disabled when there are zero notifications;
- enabled when notifications exist, even if all are read;
- shows an unread badge when unread count is greater than zero;
- changes colour when unread count is greater than zero;
- animates once when unread count changes from `0` to `1`;
- click requests the notifications side panel to open.

The bell can use the existing Font Awesome asset:

```html
<i class="fas fa-bell"></i>
```

Add a plain bottom-right "new notifications" toast in a global notification host. On unread count transition from `0` to `1`, the toast appears for a few seconds. Clicking the toast opens the notification side panel.

Accessibility and motion:

- use a real `button`;
- include an `aria-label` that reflects unread count;
- expose the badge count text to assistive technology;
- use `@media (prefers-reduced-motion: reduce)` to disable ring/toast animations and transitions.

## Stage 2: Notifications side panel

Add a `NotificationHost` component to `MainLayout` so the toast and side panel are available on every page.

The host should:

- subscribe to notification service state changes;
- render the bottom-right toast;
- own a `SidePanel` reference for notifications;
- open the side panel when the service receives an open request.

On side-panel opening:

- mark all notifications read;
- clear unread count;
- return bell colour to default;
- keep the bell enabled while notifications exist.

The side panel should render a table using existing table styling.

Columns:

- Message
- Location
- Severity/type
- Read checkbox
- Time of message

The message column should include grouped count when `Count > 1`, for example `x3`. If `ActionUrl` exists, the message or a small action link can navigate to it, such as "view location".

If there are no notifications, show a compact empty state inside the panel.

## Stage 3: Replace snackbar usage

Replace `SnackbarMessage` with notification-specific types.

Migration approach:

- introduce a temporary compatibility helper if needed, but do not keep `SnackbarMessage` as the long-term user-message model;
- change chart services to return notification drafts or user messages that do not reference Blazorise;
- inject/use `INotificationService` from `ChartablePage`;
- replace `SnackbarMessageEventHandler` with notification service calls;
- remove page-level `SnackbarStack` components from `Index.razor` and `RegionalAndGlobal.razor`;
- remove `@using Blazorise.Snackbar` from migrated files;
- remove the snackbar CSS preload from `App.razor` when no longer needed;
- remove snackbar override CSS from `app.css`;
- delete `SnackbarMessage.cs` after all references are removed.

Initial migration targets:

- chart build failure: `Error`;
- no data available for preset: `Warning` or `Error`;
- data unavailable at changed location: `Warning`, with location context;
- completeness threshold removed all observations: `Warning`, with location context when available;
- moving-average fallback to unsmoothed data: `Warning`;
- invalid chart URL state: candidate `Warning`, but only if surfaced in a way that helps the user recover;
- recent-observations load failure: candidate `Warning` or `Error`, but avoid duplicating the existing inline panel error unless a global notification adds value.

Messages that should stay logs:

- cache hit/miss;
- data retrieval start/end;
- chart rendering internals;
- temporary-file cleanup warnings unless they affect the user-visible result;
- backend operational warnings that are already represented by an API response or inline UI state.

## Notification actions

Notification actions should be optional. The first useful action is "view location" for location-specific notifications.

Suggested fields:

- `ActionText`
- `ActionUrl`

The side panel table should render the action only when both are present. The notification service should not know how to build every action URL; callers that have page context should supply it.

## Session lifetime

Use scoped in-memory state. In Blazor WebAssembly this behaves as app-session state. In interactive server/prerender handoff paths, the state should be treated as best-effort session UI state rather than durable history.

Do not persist notifications to local storage. This keeps the requirement "notifications are cleared when the session ends" simple and avoids stale diagnostics appearing in a later visit.

## Styling notes

Bell colours should fit the existing nav palette:

- default: white or current nav link colour;
- disabled: muted white with no pointer cursor;
- unread: use the existing info blue or a warning colour with enough contrast.

The toast should be bottom-right, above page content, and not use Blazorise snackbar classes. It should be small, clickable, and dismiss automatically after a few seconds.

The side panel can reuse the existing `SidePanel` animation. If needed, add reduced-motion handling to `SidePanel.razor.css` as part of Stage 2 because notifications are explicitly motion-sensitive.

## Testing plan

Unit tests for notification service:

- adding first unread notification changes unread count from 0 to 1;
- identical notifications group and increment count;
- grouped notification becomes unread when repeated;
- `MarkAllRead` clears unread count but leaves notifications;
- bell disabled/enabled state can be derived from total count;
- action/link fields survive grouping;
- open-panel request event is raised.

Component/UI tests or manual verification:

- desktop bell appears as the last nav item;
- mobile bell appears immediately to the left of the hamburger;
- badge appears only when unread count is greater than zero;
- bell remains enabled after panel opens when read notifications exist;
- toast click opens side panel;
- bell click opens side panel;
- opening side panel marks notifications read;
- reduced-motion disables ring/toast animation.

Build verification:

- run `dotnet build`;
- run `dotnet test`;
- if UI tests are available and practical, run the relevant nav/chart smoke tests.

## Risks and follow-ups

The current nav is Blazorise-based. The notification UI can be plain HTML/CSS within that nav, but spacing and mobile behavior should be checked carefully because existing CSS uses `last-child` selectors for nav layout.

There may be subtle differences between transient snackbar deduplication and persistent grouped notifications. Grouping should be deterministic and tested before replacing snackbar behavior.

Some current log messages may look tempting to surface but would create noisy notifications. Stage 3 should migrate only user-actionable or user-diagnostic messages first.

The existing `SidePanel` has animation timing in C# and CSS. Reduced-motion support may require changing both the CSS and the component delay behavior if the visual transition is removed.

## Open questions

- Should unread count count grouped occurrences or grouped rows?
  - Answer: count occurrences, while the table shows grouped rows with `Count`.
- Should notification read checkboxes be editable by the user after all notifications are marked read on panel open?
  - Answer: yes, but the initial implementation can make the checkbox a read-state indicator if editing is not valuable.
- Should the toast appear on every new unread notification or only on the `0 -> 1` transition?
  - Answer: only on `0 -> 1`, matching the requested behavior and reducing noise.
- Should recent-observations inline failures also become global notifications?
  - Answer: not in the first migration unless the failure affects page-level understanding outside the panel.

## Addendum - implementation notes

Stage 1 was implemented on 2026-06-26.

What shipped:

- Added session-scoped notification models and `IUserNotificationService`/`NotificationService`.
- Added grouped notification behavior with unread counts based on grouped occurrences.
- Added a first-unread transition event for the `0 -> 1` unread-count case.
- Added a plain `NotificationBell` component with badge, disabled state, unread colour, one-shot ring animation, and reduced-motion CSS.
- Added a plain `NotificationHost` component that shows a bottom-right "New notifications" toast for the first-unread transition.
- Wired the bell into `NavMenu` after desktop secondary items and immediately before the mobile hamburger item.
- Wired the toast host into `MainLayout`.
- Registered the notification service in client DI.
- Added unit tests for first-unread events, grouping, read-state clearing, action URL grouping, and open-panel requests.

Intentional Stage 1 boundary:

- The bell and toast request the notifications panel through the service, but the side panel itself remains Stage 2 work.
- Existing snackbar producers were not migrated; that remains Stage 3 work.

Implementation note:

- The service interface is named `IUserNotificationService` to avoid colliding with Blazorise's own `INotificationService`.

Stage 2 was implemented on 2026-06-26.

What shipped:

- Added the notifications `SidePanel` to `NotificationHost`.
- Bell and toast clicks now open the side panel through the notification service's open-panel request event.
- Opening the side panel marks all notifications read, clearing the unread count and returning the bell to its default colour while keeping it enabled.
- Added a notifications table with message, location, severity/type, read checkbox, and message time columns.
- Grouped notifications show an `xN` count in the message column.
- Optional notification actions render as links when `ActionText` and `ActionUrl` are present.
- Added editable read checkboxes backed by `NotificationService.SetRead`.
- Added an empty state for sessions with no notifications.
- Added reduced-motion CSS for the existing side panel transition.
- Added unit tests for per-notification read state updates.

Intentional Stage 2 boundary:

- Existing snackbar producers still have not been migrated into notifications; that remains Stage 3 work.
