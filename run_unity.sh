#!/bin/bash
UNITY_PATH="/c/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
if [ ! -f "$UNITY_PATH" ]; then
  UNITY_PATH=$(which Unity.exe)
fi
if [ -z "$UNITY_PATH" ]; then
  UNITY_PATH="/c/Users/frcor/AppData/Local/Unity/bin/Unity.exe"
fi
"$UNITY_PATH" -batchmode -nographics -projectPath "$(cygpath -w "$(pwd)")" "$@"
EXIT_CODE=$?
echo "Exit code: $EXIT_CODE"
exit $EXIT_CODE
