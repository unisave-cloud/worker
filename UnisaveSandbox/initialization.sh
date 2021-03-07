#!/usr/bin/env bash

# This script is the entrypoint of the function container.
# It performs initialization and then starts the watchdog.

mono --debug /sandbox/UnisaveSandbox.exe init $INITIALIZATION_RECIPE_URL

fwatchdog
