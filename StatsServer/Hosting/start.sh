#!/bin/bash
set -euo pipefail

. $HOME/bin/env.sh

TODAY=$(date -I)

cd "$PL_SERVER_DIR"
dotnet StatsServer.dll >>"${PL_LOG_DIR}/${TODAY}.jsonl" 2>&1 &