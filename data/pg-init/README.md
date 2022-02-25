This is a Placeholder file. Do not delete

Place here the dump that will create the database from scratch and any other sql scripts needed to create the database.
The docker image for PostgreSQL will scan this folder only upon creation of the container and not on every run.
Files will be searched by their .sql extension and executed ordered by file name 
So if for example we need to run a script after the dump with name that starts with A do something like this

- 01_create_database.sql
- 02_Authors_insertdata.sql