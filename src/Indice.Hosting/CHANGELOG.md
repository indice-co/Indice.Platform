# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
### Added
- Added the ability to configure the `MaxRetryCount` for a failed message when using Queue Triggers to specify the number of times the message is re-enqueued before being moved to the poison queue.
- Added the ability to configure `WaitJobsToCompleteOnShutdown` on `AddWorkerHost` to specify whether the server should wait for all jobs to complete before shutting down.

### Fixed
- Trying to resolve an unregistered service dependency from the DI in a job will now throw an exception instead of returning null. Make sure no unregistered dependencies are used in your Jobs.

### Changed
- Default retry times for failed messages in Queue Triggers now defaults to **3** instead of **5** as a more sensible default. Clients that used the previous value as a feature should use the new `RetryCount` option.
- Cancellation Token will be correctly passed to Handlers during Host Shutdown.