#!/usr/bin/env python3

import os
import sys
import math
import shutil
import subprocess
from os import path


CHUNK_SIZE = 50
OVERLAP = 10


def parse_args():
    if len(sys.argv) <= 1:
        print("usage: %s <video>")
        sys.exit(1)

    return sys.argv[1]


def rm_mkdir(dir_name):
    if path.isdir(dir_name):
        shutil.rmtree(dir_name)

    os.mkdir(dir_name)


def extract_subtitles(video_file, all_frames_dir):
    cmd = ["ffmpeg", "-i", video_file, path.join(all_frames_dir, "positions.srt")]
    print("cmd %s" % cmd)
    subprocess.run(cmd)


def split_frames(video_file, all_frames_dir):
    rm_mkdir(all_frames_dir)

    cmd = ["ffmpeg", "-i", video_file, path.join(all_frames_dir, "%04d.jpg")]
    subprocess.run(cmd)


def frame_nums():
    n = 0
    while True:
        yield int(round(n * (30000.0 / 1001.0))) + 1
        m = float(n) + .5
        yield int(round(m * (30000.0 / 1001.0))) + 1

        n += 1


def chunks(all_frames_dir):
    def frame_src_filenames(all_frames_dir):
        frame_src_files = []
        for frame_num in frame_nums():
            src = path.join(all_frames_dir, "%04d.jpg" % frame_num)
            if not path.exists(src):
                print("last file is '%s'" % frame_src_files[len(frame_src_files) - 1])
                break

            frame_src_files.append(src)

        return frame_src_files

    def chunk_offsets(num_frames):
        num_chunks = math.ceil(float((num_frames - OVERLAP)) / (CHUNK_SIZE - OVERLAP))
        total_length = ((CHUNK_SIZE - OVERLAP) * num_chunks) + OVERLAP
        overhang = total_length - num_frames

        adjust = [0] * (num_chunks - 1)
        i = 0
        while sum(adjust) < overhang:
            adjust[i] += 1
            i = (i + 1) % len(adjust)

        adjust = [0] + adjust
        offset = []
        prev = 0
        for a in adjust:
            prev += a
            offset.append(prev)

        return [a-b for a,b in zip(range(1, num_frames, CHUNK_SIZE - OVERLAP), offset)]

    src_frames = frame_src_filenames(all_frames_dir)
    for chunk_start in chunk_offsets(len(src_frames)):
        yield [src_frames[i-1] for i in range(chunk_start, chunk_start + CHUNK_SIZE)]


def pick_photogrammetry_frames(all_frames_dir):
    chunk_num = 0
    serno = 0

    for chunk_files in chunks(all_frames_dir):
        chunk_dir = path.join(all_frames_dir, "chunk%s" % chunk_num)
        rm_mkdir(chunk_dir)

        for src in chunk_files:
            dest = path.join(chunk_dir, path.basename(src))
            print("%s -> %s" % (src, dest))
            shutil.copyfile(src, dest)

#            cmd = ["exiftool", "-TagsFromFile", "/home/boris/Desktop/FalafelParking/DJI_0002.JPG", dest]
#            print("cmd %s" % cmd)
#            x = subprocess.run(cmd)
#            print(x)

#            cmd = ["exiftool", "-serialnumber=%s" % serno, "/home/boris/Desktop/FalafelParking/DJI_0002.JPG", dest]
#            print("cmd %s" % cmd)
#            x = subprocess.run(cmd)
#            print(x)

            serno += 1


        chunk_num += 1


video_file = parse_args()
all_frames_dir, _ = path.splitext(video_file)
photogram_frames_dir = path.join(all_frames_dir, "photogram")
split_frames(video_file, all_frames_dir)
extract_subtitles(video_file, all_frames_dir)
pick_photogrammetry_frames(all_frames_dir)
