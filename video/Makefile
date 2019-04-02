CFLAGS := $(shell PKG_CONFIG_PATH=$(FFMPEG_INST_DIR)/lib/pkgconfig pkg-config --cflags libavcodec)
LDFLAGS := $(shell PKG_CONFIG_PATH=$(FFMPEG_INST_DIR)/lib/pkgconfig pkg-config --libs libavcodec libavformat libavdevice libswresample libswscale)

libvideo.so: recorder.c
	gcc -shared -fPIC $(CFLAGS) recorder.c -Wl,-Bsymbolic $(LDFLAGS) -o libvideo.so

recorder: recorder.c
	gcc -g $(CFLAGS) -DRECORDER_WITH_MAIN_FUNC recorder.c $(LDFLAGS) -o recorder

clean:
	rm -rf libvideo.so recorder
