# Direction
- [ ] Decide if this will be a microfrontend an angular library or a separate deployed SPA.
- [ ] If this is a separate SPA then it makes sense for the Officer to log into a single dashboard. Make the SPA support multiple storages per organisation. We cannot offer 5 different dashboards for each service that uses workers in a separate database.

# TODO - before release
- [ ] Correct authentication and authorization for the UI - currently it is a bit of a mess.
- [ ] Rethink all queries/indices performed on the real time db e.g. Min auto refresh 30 seconds + others - drop low value + spamming
- [ ] Rethink models e.g. stats model to see what actually makes sense
- [ ] Add tests

# Improvements
## Easy wins
- [ ] Edit the Payload then requeue.
- [ ] Add different read/write scopes.
- [ ] Enqueue a new queue message from the dashboard
- [ ] Force delete/release lock in UI.
- [ ] Add health checks reporting poison messages above a threshold. If "stuck" scheduled tasks is implemented this too.

## Advanced
- [ ] Introduce host registration - with a ping - to check heartbeat/liveness. Can also mark stale servers without the additional table provided we write in queues - in scheduled tasks we do - the server name so we can deduce liveness somehow. Or even better with a heartbeat table with e.g. instanceId and LastHeartbeat.
- [ ] Add ability to `RunNow` on Scheduled jobs
- [ ] Flag "stuck" tasks in the UI - show time running above a value if no alternative with current db - should not have
- [ ] Known bug where last error is not cleared and shows up in the UI.
- [ ] GetDate known issue db column does not include timezone - need to to hacks in spa
- [ ] Think about adding otel counter + gauge only as a minimum

## Advanced advanced - Hangfire and internal discussions inspired
- [ ] Replace quartz with an in hous database table. Then we can cancel a running task, pause execution for some time, have better reports on what is really running.
- [ ] Run history and metrics/etl/analytics based on that.
- [ ] User can create notification/alerts from the Dashboard for failed/stuck tasks etc.
- [ ] Capture the logs of a run and display them in UI for Officer. 
- [ ] Have better handling of queue messages if we replace delete with a lock query - updlock, rowlock, readpast
