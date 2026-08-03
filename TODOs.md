TODOs
-----

- [ ] Navigation: When user is not signed in (in /register), the navigation shows "dashboard", "user information" and "logout" links. These should not be visible when the user is not signed in.
- [ ] Version number: fix the version number on /login page.
- [ ] Localizer: Localizer is part of _Imports.razor. We should remove it from each page since it is already imported globally.
- [ ] Ressources: Rework the resources by adding [Page]_[Resource] naming convention to avoid conflicts and improve organization.
- [ ] Overview: Create an overview documentation that shows what page links to what other pages. This will help developers understand the navigation flow and dependencies between pages.
- [ ] Buttons: streamline the button components to reduce redundancy and improve maintainability. Consider creating a base button component that can be reused across different pages.
- [ ] Icons: Review the icons in AppIcons.cs to ensure a clear association between the icon names and their actual usage in the application. Remove any unused icons to keep the codebase clean.
    - [ ] Check /config/icons page (linked in navigation)
    - [ ] Check /style page (hidden page)
- [ ] 


