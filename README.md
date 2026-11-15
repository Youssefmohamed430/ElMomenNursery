# ElMomenNursery

## Development logs

During development, the application writes rolling log files to `Logs/` beside
`ElMomenNursery.sln`. When running outside the solution tree (for example, an
installed build), logs are written to `Logs/` beside the application instead.
The location and retention limits can be changed under `Serilog:File` in
`ElMomenNursery/appsettings.json`. Logs include exceptions and operation context;
passwords are not written to the log.
