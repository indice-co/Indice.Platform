# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [6.5.1] - 2023-01-10
## Fixed
- Angular build

## [6.5.0] - 2023-01-24
## Added
- Support for net7
## Changed
- Changes following the API refactoring in dto names
- `JSON.stringify(data)` is no longer needed to send json data to a case. Case data request is dynamic.
- `JSON.parse(data)` is no longer needed for parsing. Case data response is dynamic.
## Removed
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
Jumped to 6.4.0 to match other Indice Packages
