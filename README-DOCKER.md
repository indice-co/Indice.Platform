# Getting Started
## Prerequisites
### Download Docker
You can download Docker from:
https://www.docker.com/products/docker-desktop
### Download WSL2
The project is designed to work on linux distro therefore the containers require the Windows Subsystem for Linux to run on a Windows machine.

The following quickstart guide provides all the information to do so:
https://docs.microsoft.com/en-us/windows/wsl/install-win10

## Running PostgreSQL
All APIs and services work with a PostgreSQL as a backing database. For development purposes we are running [PostgreSQL](https://www.postgresql.org/) 12.2 and
[pgAdmin](https://www.pgadmin.org/) 4.19 by using docker containers. In order to start these containers, navigate to the project's folder and just enter the following command in your terminal.

``` powershell
docker-compose --project-name indice-aspnet up -d
```

This will automatically download the required images (if not available), setup a server with a user account and start the containers.

When the operation finishes navigate to [http://localhost:5050](http://localhost:5050). This hosts the pgAdmin interface. Please use the following credentials:
``` 
username: indice_user@pgadmin.org
password: 123abc!
```

When trying to access the preloaded server you will be prompted to enter a password for the user account `indicedb`. The password you have to use is `1nD1c3_@`.