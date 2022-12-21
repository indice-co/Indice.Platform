#!/usr/bin/env bash

# Clean and build in release
dotnet restore
dotnet clean
dotnet build -c Release
