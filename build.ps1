#!/usr/bin/env bash

dotnet restore /nowarn:netsdk1138
dotnet clean
dotnet build --configuration Release