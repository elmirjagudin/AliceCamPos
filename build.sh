#!/bin/sh


STREAMING_ASSETS_DIR="$(pwd)/unity/Assets/StreamingAssets"
PLUGINS_DIR="$(pwd)/panopt/Assets/Plugins"
PROJ_SRC_DIR="$(pwd)/proj.4"

BUILD_DIR="$(pwd)/build"
PROJ_BUILD_DIR="$BUILD_DIR/proj"
FFMPEG_INST_DIR="$BUILD_DIR/ffmpeg"

## build proj.4
#mkdir -p "$PROJ_BUILD_DIR"
#cd "$PROJ_BUILD_DIR"
#cmake -DBUILD_LIBPROJ_SHARED=ON -DCMAKE_BUILD_TYPE=Debug "$PROJ_SRC_DIR"
#make
#mkdir -p "$PLUGINS_DIR"
#cp "$PROJ_BUILD_DIR/lib/libproj.so" "$PLUGINS_DIR"
#cp "$PROJ_BUILD_DIR/data/proj.db" "$STREAMING_ASSETS_DIR"
#cd -

# build ffmpeg
mkdir -p "$FFMPEG_INST_DIR"
cd ffmpeg
./configure --prefix=$FFMPEG_INST_DIR --enable-pic --enable-gpl --enable-libx264
make install
cd -

# build video wrapper lib
cd video
make libvideo.so FFMPEG_INST_DIR="$FFMPEG_INST_DIR"
mkdir -p "$PLUGINS_DIR"
cp libvideo.so "$PLUGINS_DIR"


