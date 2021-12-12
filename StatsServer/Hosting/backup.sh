#!/bin/bash
set -euo pipefail

echo "---"
date -Is
echo "---"

. $HOME/bin/env.sh

# Backup today's ZIP archive
cd "$PL_BACKUP_DIR"
TODAY=$(date -I)
FILENAME="PluginLoaderStatsData.${TODAY}.zip"
rclone copy "${FILENAME}" "${PL_BACKUP_REMOTE_NAME}:${PL_BACKUP_REMOTE_DIR}/"
rm "${FILENAME}"

# Remove old backups only the the latest backup above succeeded (otherwise this script has failed already)
# It is normal for this step to fail until we have enough past backups to have something to delete.
#REMOVE_DATE=$(date -I --date="yesterday")
REMOVE_DATE=$(date -I --date="a week ago")
REMOVE_FILENAME="PluginLoaderStatsData.${REMOVE_DATE}.zip"
rclone delete "${PL_BACKUP_REMOTE_NAME}:${PL_BACKUP_REMOTE_DIR}/${REMOVE_FILENAME}"
