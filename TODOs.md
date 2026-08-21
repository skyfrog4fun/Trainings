TODOs
---

General
---
- [ ] Check all pages for wrong texts from Localizer (e.g. in UserInformation, ...)

Design Groups Pages
---
- [ ] Rework the idea how to navigate inside the groups
    - [ ] all pages uses {slug} for navigation
    - [ ] overview (list) -> new | details of group -> edit | add member | delete
        - [ ] overview (/groups): list of groups (only possibility to go into details (/groups/{slug}) => see detail at end of this file
- [ ] 
- [ ] Groups (/groups):
- [ ] Create Group (/groups/new):
- [ ] Edit Group (/groups/2/edit):
- [ ] Group Members (/groups/{slug}/members):

Mail
---
- [ ] Mails are sent in English only. There's no localization for mails. Add localization for mails (EN/DE). Check if the mail templates are used in the code and if they are available in the resx files. If not, add them to the resx files.

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

---

Done - General
---
- [X] Navigation: When user is not signed in (in /register), the navigation shows "dashboard", "user information" and "logout" links. These should not be visible when the user is not signed in.
- [X] Version number: fix the version number on /login page.

Done - Localization
---
- [X] Localizer: Localizer is part of _Imports.razor. We should remove it from each page since it is already imported globally.
- [X] Ressources: Rework the resources by adding [Page]_[Resource] naming convention to avoid conflicts and improve organization.
- [X] Replace all text on all pages with a created from the resx file.
- [X] Structure the pages into subfolders for better organization. For example, group related pages together based on their functionality or feature set.
- [X] Harmonize naming of Razor pages. Do all pages need to end with 'Pages.razor' or can we remove this suffix?
- [X] Search on all pages for Localizer[".."] and check if pattern [Page]_[Resource] is used. If not, change it to the new pattern. See [LocalizationAudit.md](LocalizationAudit.md) for the full audit report (violations and shared-key candidates still need to be renamed as a follow-up).
- [X] Check if all found pattern [Page]_[Resource] on pages are available on RESX files. If not, add them to the RESX file.
- [X] Remove all text on RESX files that not not match the pattern [Page]_[Resource] and/or are not used on any page.
- [X] Properly translate all the text on the RESX files from EN to DE.


Notes
---

Example on how to structure the navigation for copilot chat?!

/groups
  ├── New
  │    └── /groups/new
  │
  └── Group detail
       └── /groups/{slug}
              ├── Update
              │    └── /groups/{slug}/update
              │    
              ├── Delete
              │
              └── Manage Members
                   └── /groups/{slug}/members
