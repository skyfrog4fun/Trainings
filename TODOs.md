TODOs
---

General
---
- [ ] Check all pages for wrong texts from Localizer (e.g. in UserInformation, ...)

Bugs / Changes
---
- [ ] Location / New Location: Country has 'Country - Code'. Change to show only country name.
- [ ] User / Create User: pre select country and set it to the country of the signed in user.
- [ ] User / Create User: Groups are shown even country is not selected. I should only see groups from the selected country.
- [ ] User / Create User: SuperAdmin creates a new user and clicks on 'Verify'. Mail is displayed in browser in dev environment. When clicking the link, the user is shown as 'Email verified: Yes', but has no entry date.




User Information
---
- [ ] UserDetailPage: rework the layout of the page to make it more user-friendly and intuitive.


Design User Pages
---
- [ ] Users: sorting (by name?), searching (by name?), paging (10, 20, 50, 100)
- [ ] Selection Color is blue, but it shouldn't. Compare with Location page.
- [ ] ...


Design Group Pages
---
- [ ] Group / New Group: Locations shows strange sign in selection 'Location name â€“ City'
- [ ] Group / New Group: remove Localized time below 'Start' and 'End'
- [ ] Group / New Group: Country list should show name only, remove 'Country - Code'
- [ ] Group / New Group: select country first (pre select from XY) and then show only locations from that country
- [ ] Group / List: large group description needs to be handled better so that each card has equal height and the text is truncated with '...' if it exceeds the card height. This will improve the visual consistency of the group list.
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
- [ ] Search for users on page /users: add search by email, first name, last name, username, etc.
- [ ] Overview: Create an overview documentation that shows what page links to what other pages. This will help developers understand the navigation flow and dependencies between pages.
- [ ] Documentation: create an authorization overview. What role can do what
- [ ] How can a user request to participate in a new group? [UserInformation -> extend section 'Group Memberships']



---

Done - Reworked Sections
- [X] Locations
- [X] User Information
- [ ] Users (List, Details, Edit/New)

Done - Tasks
---
- [X] Navigation: When user is not signed in (in /register), the navigation shows "dashboard", "user information" and "logout" links. These should not be visible when the user is not signed in.
- [X] Version number: fix the version number on /login page.
- [X] Localizer: Localizer is part of _Imports.razor. We should remove it from each page since it is already imported globally.
- [X] Ressources: Rework the resources by adding [Page]_[Resource] naming convention to avoid conflicts and improve organization.
- [X] Replace all text on all pages with a created from the resx file.
- [X] Structure the pages into subfolders for better organization. For example, group related pages together based on their functionality or feature set.
- [X] Harmonize naming of Razor pages. Do all pages need to end with 'Pages.razor' or can we remove this suffix?
- [X] Search on all pages for Localizer[".."] and check if pattern [Page]_[Resource] is used. If not, change it to the new pattern. See [LocalizationAudit.md](LocalizationAudit.md) for the full audit report (violations and shared-key candidates still need to be renamed as a follow-up).
- [X] Check if all found pattern [Page]_[Resource] on pages are available on RESX files. If not, add them to the RESX file.
- [X] Remove all text on RESX files that not not match the pattern [Page]_[Resource] and/or are not used on any page.
- [X] Properly translate all the text on the RESX files from EN to DE.
- [X] LocationsPage: Group Assignment (Access): in DE the 'weekdays' are not written in selected language. for DE it shows 'Tuesday'
- [X] LocationsPage: Visualization: in DE the 'weekdays' are not written in selected language. for DE it shows 'Tuesday'
- [X] LocationsPage: Visualization of dropdown fixed (coloring, etc...)
- [X] Edit User: visually style the form to make it more user-friendly and intuitive. (Issue: Dark Mode color of placeholder)
- [X] UserInformation: Reworked
- [X] User Detail (/users/{id}) - Admin View: rework
- [X] User Edit (/users/{id}/edit) - Admin View: rework
- [X] Back button on 'Create User' has a strange green when pressed and hold
- [X] Method 'ValidatePassword' still used on UserInformationPage?
- [X] Is validation redundant? UserFormModel.cs vs. PasswordGenerator.cs
- [X] Think about: UserFormModel.cs Localization Strings? ... should they come from Shared?!
- [X] UserInformationPage: Gender has no German translation
- [X] UserInformationPage: fix localization in Group Memberships section (EN/DE)
- [X] Registration: Send mail to GroupAdmin for new registrations
- [X] User Overview (/users) - Admin View: rework


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
