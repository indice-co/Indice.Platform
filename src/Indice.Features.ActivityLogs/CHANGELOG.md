# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [8.62.0]
### Fixed
- The activity log cleanup (`IActivityLogStore.Cleanup`) now works on PostgreSQL. It used `EF.Functions.DateDiffDay`, which only SQL Server can translate. The query is now chosen by `Database.ProviderName`: SQL Server keeps `DATEDIFF(day, ...)`, and PostgreSQL compares `CreatedAt` with a UTC midnight cutoff. Both delete the same rows.
