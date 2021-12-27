#!/bin/bash
set -euo pipefail

echo "---"
date -Is
echo "---"

. $HOME/bin/env.sh

# Remove old log files
cd "$PL_LOG_DIR"
for FN in $(find . -mtime +60 | grep '.jsonl'); do
	echo "Removing: $FN"
done

# Compress past log files
TODAY=$(date -I)
for FN in $(find . | egrep '.jsonl$' | grep -v $TODAY); do
	echo "Compressing: $FN"
	nice gzip -9 $FN
done
