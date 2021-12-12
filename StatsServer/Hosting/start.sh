#!/bin/bash
set -euo pipefail

. $HOME/bin/env.sh

TODAY=$(date -I)

cd "$PL_SERVER_DIR"
dotnet StatsServer.dll >>"${PL_LOG_DIR}/${TODAY}.log" 2>&1 &
