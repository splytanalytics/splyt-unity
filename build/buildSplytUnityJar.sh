#!/bin/sh

# see if build type was provided
buildtype=release
if [ -n "$1" ]; then
  buildtype=$1
fi

pushd $(dirname "$0") >/dev/null
pushd ../android >/dev/null
rm libs/classes.jar
ln -s /Applications/Unity/Unity.app/Contents/PlaybackEngines/AndroidPlayer/$buildtype/bin/classes.jar libs/classes.jar
ant clean $buildtype
cp bin/SplytUnity.jar ../samples/BubblePop/Assets/Plugins/Android/SplytUnity.jar
popd >/dev/null
popd >/dev/null


