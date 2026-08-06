TODOs
---

General
---

Localization
---
- [X] Localizer: Localizer is part of _Imports.razor. We should remove it from each page since it is already imported globally.
- [X] Ressources: Rework the resources by adding [Page]_[Resource] naming convention to avoid conflicts and improve organization.
- [X] Replace all text on all pages with a created from the resx file.
- [ ] Structure the pages into subfolders for better organization. For example, group related pages together based on their functionality or feature set.
- [ ] Search on all pages for Localizer[".."] and check if pattern [Page]_[Resource] is used. If not, change it to the new pattern.
- [ ] Check if all found pattern [Page]_[Resource] on pages are available on RESX files. If not, add them to the RESX file.
- [ ] Remove all text on RESX files that not not match the pattern [Page]_[Resource] and/or are not used on any page.
- [ ] Properly translate all the text on the RESX files from EN to DE.

Registration
---
- [ ] Validate all fields in the UI (validate what is missing). Compare pages 'Register', 'UserInformation', 'UserDetail' and 'CreateEditUser'.

Security
---
- [ ] Password reset (at least on debug/localhost) does not work. The link points to somewhere in the internet (Brevo page!?).

Others
---
- [ ] Buttons: streamline the button components to reduce redundancy and improve maintainability. Consider creating a base button component that can be reused across different pages.
- [ ] Icons: Review the icons in AppIcons.cs to ensure a clear association between the icon names and their actual usage in the application. Remove any unused icons to keep the codebase clean.
    - [ ] Check /config/icons page (linked in navigation)
    - [ ] Check /style page (hidden page)
- [ ] Create a SKILL to update packages with minial changes. Check for breaking changes and update the code accordingly. This will help keep the application up-to-date with the latest dependencies while minimizing potential issues. Verify that the application keeps running.

Ideas
---
- [ ] Overview: Create an overview documentation that shows what page links to what other pages. This will help developers understand the navigation flow and dependencies between pages.

Done
---
- [X] Navigation: When user is not signed in (in /register), the navigation shows "dashboard", "user information" and "logout" links. These should not be visible when the user is not signed in.
- [X] Version number: fix the version number on /login page.
