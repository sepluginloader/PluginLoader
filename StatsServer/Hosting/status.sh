#!/bin/bash
set -euo pipefail

. $HOME/bin/env.sh

if killall -0 dotnet >/dev/null 2>&1; then
	echo "Running"
else
	echo "Stopped"
fi