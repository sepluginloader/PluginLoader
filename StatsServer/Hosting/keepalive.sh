#!/bin/bash
set -euo pipefail

. env.sh

CANARY_URI="${PL_BASE_URI}/Canary"
CANARY_REQUEST="--max-time ${PL_TIMEOUT} ${CANARY_URI}"

if curl -s $CANARY_REQUEST >/dev/null; then
	exit 0
fi

echo "---"
date -Is
echo "---"

if curl -sS $CANARY_REQUEST; then
	echo Server still works. Second Canary attempt succeeded.
	exit 0
fi

echo Server is down. Attempting to restart it.
bash ~/bin/restart.sh
