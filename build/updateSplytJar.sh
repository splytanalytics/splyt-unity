#!/bin/sh
pushd $(dirname "$0") >/dev/null
BASEDIR=$(pwd -P)
cp "$BASEDIR/../../android/libs/splyt/bin/splyt-android.jar" "$BASEDIR/../android/libs/splyt-android.jar"
popd >/dev/null


