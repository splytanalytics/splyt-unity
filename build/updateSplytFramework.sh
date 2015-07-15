#!/bin/sh
pushd $(dirname "$0") >/dev/null
BASEDIR=$(pwd -P)
rsync "$BASEDIR/../../ios/framework/Splyt.framework" "$BASEDIR/../samples/BubblePop/Assets/Plugins/iOS/" --copy-links -a
popd >/dev/null


