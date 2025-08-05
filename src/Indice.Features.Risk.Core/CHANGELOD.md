# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [8.10.0] - 2025-08-05
### The column `Type` in `RiskEvent` and `DbAggregateRuleExecutionResult` is now nullable
```sql
ALTER TABLE [dbo].[RiskEvent]
ALTER COLUMN [Type] [DataType] NULL;

ALTER TABLE [dbo].[RiskResult]
ALTER COLUMN [Type] [DataType] NULL;
```
