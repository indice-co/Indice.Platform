#!/usr/bin/env bash

dotnet restore
dotnet clean
dotnet build --configuration Release