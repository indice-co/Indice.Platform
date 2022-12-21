#!/usr/bin/env bash

# Clean and build in release
dotnet restore /nowarn:netsdk1138
dotnet clean
dotnet build -c Release
