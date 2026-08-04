TODOs
-----

- [X] Navigation: When user is not signed in (in /register), the navigation shows "dashboard", "user information" and "logout" links. These should not be visible when the user is not signed in.
- [X] Version number: fix the version number on /login page.
- [X] Localizer: Localizer is part of _Imports.razor. We should remove it from each page since it is already imported globally.
- [ ] Ressources: Rework the resources by adding [Page]_[Resource] naming convention to avoid conflicts and improve organization.
- [ ] Replace all text on all pages with a created from the resx file.
- [ ] RESX file: clean up all the old termns (That should be the once not fulfilling the defined standard)
- [ ] Registration: Add all fields to the UI (validiate what is missing)
- [ ] Put pages in subfolders for better overview. Find out what grouping make most sense.
- [ ] Overview: Create an overview documentation that shows what page links to what other pages. This will help developers understand the navigation flow and dependencies between pages.
- [ ] Buttons: streamline the button components to reduce redundancy and improve maintainability. Consider creating a base button component that can be reused across different pages.
- [ ] Icons: Review the icons in AppIcons.cs to ensure a clear association between the icon names and their actual usage in the application. Remove any unused icons to keep the codebase clean.
    - [ ] Check /config/icons page (linked in navigation)
    - [ ] Check /style page (hidden page)
- [ ] 
