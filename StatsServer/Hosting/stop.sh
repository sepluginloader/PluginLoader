#!/bin/bash
set -euo pipefail

if ! killall dotnet >/dev/null 2>&1; then
    echo "Not running"
    exit 0
fi

for NN in 10 9 8 7 6 5 4 3 2 1; do
    echo "Waiting ${NN}"
    sleep 1
    if ! killall dotnet >/dev/null 2>&1; then
        echo "Stopped"
        exit 0
    fi
done

if killall -9 dotnet >/dev/null 2>&1; then
    echo "Killed"
else
    echo "Stopped"

exit 0