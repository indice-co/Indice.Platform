# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [6.10.6] - 2023-03-13
### Changed
- Redesigned the create case side panel. Removed the drop down for picking a case type. Now it displays all the available case types. Made the title dynamically change based on the wizard step. Minor design changes for picking customer number. Created a new component for displaying all the available case types.

##  [6.10.5] - 2023-03-02
### Changed
- Case type form for create/update is simplified. Only case type entity is being edited.

## [6.10.4] - 2023-03-01
### Changed
- `notifications` route can only be seen & navigated by non-admin users
- use `lib-toggle-button` component from `@indice/ng-components` npm package
### Fixed
- modal bugs introduced from `@indice/ng-components` npm package latest version

## [6.10.1] - 2023-02-15
### Added
- Dashboard with stats, filtered by role

## [6.9.1] - 2023-02-07
### Changed
- Notification Subscriptions are now per case type

## [6.6.0] - 2023-01-30
- Jumped to 6.6.0 to match other Indice Packages

## [6.5.1] - 2023-01-24
### Fixed
- Angular build

## [6.5.0] - 2023-01-24
### Added
- Support for net7
### Changed
- Changes following the API refactoring in dto names
- `JSON.stringify(data)` is no longer needed to send json data to a case. Case data request is dynamic.
- `JSON.parse(data)` is no longer needed for parsing. Case data response is dynamic.
### Removed
- Support for net3.1

## [6.4.3] - 2023-01-10
### Added
- add min/max date configuration for `date-widget` through layout options
- momentjs dependency

## [6.4.2] - 2023-01-10
### Added
- a lookup-selector widget
### Fixed
- a minor typeahead bug in lookup widget

## [6.4.0] - 2022-12-22
- Jumped to 6.4.0 to match other Indice Packages
