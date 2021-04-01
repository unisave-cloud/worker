#!/usr/bin/env bash

# This script is the entrypoint of the function container.
# It performs initialization and then starts the watchdog.

# initialize the sandbox
echo "[$0]: Initializing the sandbox..."
mono --debug /sandbox/UnisaveSandbox.exe init $INITIALIZATION_RECIPE_URL
echo "[$0]: Initialization done."

# start the watchdog process
echo "[$0]: Starting the watchdog..."
fwatchdog &
PID_WATCHDOG=$!

# handle termination signals
echo "[$0]: Registering the signal handler..."
trap handle_signal 1 2 3 6 15
handle_signal()
{
	echo "[$0]: Signal received!"
	kill -s TERM ${PID_WATCHDOG} || true
}
echo "[$0]: Ready."

# wait for the watchdog to terminate
wait
