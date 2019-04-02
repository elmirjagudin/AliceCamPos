/**
 * Simple wrapper around FFMPEG API to create video files.
 *
 * Initilize new recorder object with call to recorder_init().
 *
 * Add video frames with recorder_encode_frame() and finilize the
 * video file with call to recorder_close().
 *
 * This API assumes that frames provided for encoding are in RGB24 format.
 *
 * Note: this code assumes that we are creating a MOV file using h264 codec.
 * The filename provided to recorder_init() should have .mov extension,
 * otherwise things may not work, as we rely on FFMPEG automagic selection of
 * the muxer and the codec.
 */
#include <stdlib.h>
#include <stdbool.h>
#include <stdio.h>

#include <libavformat/avformat.h>
#include <libswscale/swscale.h>

#define TEXTURE_PIX_FMT AV_PIX_FMT_RGB24
#define VIDEO_PIX_FMT AV_PIX_FMT_YUV420P


typedef struct Recorder
{
    AVFormatContext *output_ctx;
    AVCodecContext *codec_ctx;
    AVStream *stream;
    AVFrame *frame;

    struct SwsContext *sws_ctx;

    /* for pointing out input RGB24 data */
    uint8_t *pixels[1];
    int linesize[1];
}
Recorder;

/*
 * macro for checking for errors signaled via pointer
 *
 * if PTR is NULL, print ERR_MSG to stderr and return -1
 */
#define CH_PTR(PTR, ERR_MSG)                                              \
{                                                                         \
    if (PTR == NULL)                                                      \
    {                                                                     \
        fprintf(stderr, "%s:%d %s error\n", __FILE__, __LINE__, ERR_MSG); \
        return -1;                                                        \
    }                                                                     \
}

/*
 * macro for checking for errors signaled via int
 *
 * if RET is not 0, print ERR_MSG to stderr and return -1
 */
#define CH_RET(RET, ERR_MSG)                                              \
{                                                                         \
    if (RET != 0)                                                         \
    {                                                                     \
        fprintf(stderr, "%s:%d %s error\n", __FILE__, __LINE__, ERR_MSG); \
        return -1;                                                        \
    }                                                                     \
}


int
setup_frame(AVFrame **frame, int width, int height, enum AVPixelFormat pix_format)
{
    AVFrame *f = *frame = av_frame_alloc();
    CH_PTR(frame, "av_frame_alloc");

    f->pts = 0;
    f->format = pix_format;
    f->width = width;
    f->height = height;

    int ret = av_frame_get_buffer(f, 0);
    CH_RET(ret, "av_frame_get_buffer");

    return 0;
}

/*
 * Set-up video stream recorder instance.
 *
 * recorder             - the pointer where to write recorder handle
 * width                - stream frame's width, must be a multiple of two
 * height               - stream frame's height, must be a multiple of two
 * timebase_numerator   - stream's timebase numerator
 * timebase_denominator - stream's timebase denominator
 *
 * returns 0 on success
 */
int
recorder_init(Recorder **recorder,
              const char *filename,
              int width, int height,
              int timebase_numerator,
              int timebase_denominator)
{
    Recorder* rec = *recorder = calloc(1, sizeof(*rec));

    rec->sws_ctx =
        sws_getContext(width, height, TEXTURE_PIX_FMT,
                       width, height, VIDEO_PIX_FMT,
                       0, NULL, NULL, NULL);
    CH_PTR(rec->sws_ctx, "sws_getContext");

    avformat_alloc_output_context2(&rec->output_ctx,
                                   NULL,
                                   NULL,
                                   filename);
    CH_PTR(rec->output_ctx, "avformat_alloc_output_context2");

    AVOutputFormat *fmt = rec->output_ctx->oformat;

    AVCodec *codec = avcodec_find_encoder(fmt->video_codec);
    CH_PTR(codec, "avcodec_find_encoder");

    rec->stream = avformat_new_stream(rec->output_ctx, NULL);
    CH_PTR(rec->stream, "avformat_new_stream");

    rec->stream->id = rec->output_ctx->nb_streams-1;
    rec->stream->time_base =
        (AVRational) { timebase_numerator, timebase_denominator };

    rec->codec_ctx = avcodec_alloc_context3(codec);
    CH_PTR(rec->codec_ctx, "avcodec_alloc_context3");

    rec->codec_ctx->codec_id = codec->id;
    rec->codec_ctx->width    = width;
    rec->codec_ctx->height   = height;

    rec->codec_ctx->time_base = rec->stream->time_base;
    rec->codec_ctx->gop_size      = 12; /* emit one intra frame every twelve frames at most */
    rec->codec_ctx->pix_fmt       = VIDEO_PIX_FMT;

    /* Some formats want stream headers to be separate. */
    if (rec->output_ctx->oformat->flags & AVFMT_GLOBALHEADER)
    {
        rec->codec_ctx->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;
    }

    /* open the codec */
    int ret = avcodec_open2(rec->codec_ctx, codec, NULL);
    CH_RET(ret, "avcodec_open2");

    ret = setup_frame(&(rec->frame), width, height, rec->codec_ctx->pix_fmt);
    CH_RET(ret, "setup_frame");

    /* copy the stream parameters to the muxer */
    ret = avcodec_parameters_from_context(rec->stream->codecpar, rec->codec_ctx);
    CH_RET(ret, "avcodec_parameters_from_context");

    av_dump_format(rec->output_ctx, 0, filename, 1);

    ret = avio_open(&(rec->output_ctx->pb), filename, AVIO_FLAG_WRITE);
    CH_RET(ret, "avio_open");

    ret = avformat_write_header(rec->output_ctx, NULL);
    CH_RET(ret, "avformat_write_header");

    /*
     * calculate negative linesize for our input texture,
     * from it's width and pixel size (3 bytes for RGB24)
     *
     * the negative linesize is used for vertically flipping the
     * input texture, as unity give's us 'upside down' bitmaps
     */
    rec->linesize[0] = -(width * 3);

    return 0;
}

static int
write_packets(Recorder *rec)
{
    AVPacket pkt = { 0 };
    av_init_packet(&pkt);

    while (true)
    {
        int ret = avcodec_receive_packet(rec->codec_ctx, &pkt);
        if (ret == AVERROR(EAGAIN))
        {
            /* no packages ready, need more frames */
            return 0;
        }
        if (ret == AVERROR_EOF)
        {
            /* stream finished, we are done */
            return 0;
        }
        CH_RET(ret, "avcodec_receive_packet");

        av_packet_rescale_ts(&pkt, rec->codec_ctx->time_base, rec->stream->time_base);

        ret = av_interleaved_write_frame(rec->output_ctx, &pkt);
        CH_RET(ret, "av_interleaved_write_frame");
    }

    return 0;
}

/*
 * add new frame to the stream
 *
 * Note: the the frame will be fliped vertically when encoded,
 *       this is to handle that Unit's textures are 'upside-down'
 *
 * rec    - the stream recorder handle
 * pixels - frame's pixels in RGB24 format
 * pts    - frame's PTS (presentation time stamp)
 *
 * returns 0 on success
 */
int
recorder_encode_frame(Recorder *rec, uint8_t *pixels, int64_t pts)
{
    /*
     * when we pass a frame to the encoder, it may keep a reference to it
     * internally; make sure we do not overwrite it here
     */
    int ret = av_frame_make_writable(rec->frame);
    CH_RET(ret, "av_frame_make_writable");

    /*
     * read the bitmaps 'from the end' so we can
     * flip it vertically
     */
    rec->pixels[0] = pixels + (-rec->linesize[0]*(rec->codec_ctx->height-1));

    /*
     * flip and convert input bitmap
     * to suitable format for the encoder
     */
    sws_scale(
        rec->sws_ctx,
        (const uint8_t * const *)rec->pixels,
        rec->linesize,
        0,
        rec->codec_ctx->height,
        rec->frame->data,
        rec->frame->linesize);

    /* send frame to the encoder */
    rec->frame->pts = pts;
    ret = avcodec_send_frame(rec->codec_ctx, rec->frame);
    CH_RET(ret, "avcodec_send_frame");


    ret = write_packets(rec);
    CH_RET(ret, "write_packets");

    return 0;
}

static int
flush_stream(Recorder *rec)
{
    int ret = avcodec_send_frame(rec->codec_ctx, NULL);
    CH_RET(ret, "avcodec_send_frame");

    ret = write_packets(rec);
    CH_RET(ret, "write_packets");

    return 0;
}

/*
 * finilize the video file and free resources,
 * the rec handle becomes invalid after this call
 *
 * rec    - the stream recorder handle
 *
 * returns 0 on success
 */
int
recorder_close(Recorder *rec)
{
    int ret = flush_stream(rec);
    CH_RET(ret, "flush_stream");

    /* finilize file */
    ret = av_write_trailer(rec->output_ctx);
    CH_RET(ret, "av_write_trailer");
    ret = avio_close(rec->output_ctx->pb);
    CH_RET(ret, "avio_close");

    /* clean-up */
    av_frame_free(&(rec->frame));
    avcodec_free_context(&(rec->codec_ctx));
    avformat_free_context(rec->output_ctx);

    sws_freeContext(rec->sws_ctx);

    free(rec);

    return 0;
}

#ifdef RECORDER_WITH_MAIN_FUNC
static void
fill_rgb_image(uint8_t *pict,
               int width, int height, int r, int g, int b)
{
    int x, y;
    int linesize = width * 3;

    for (y = 0; y < height; y++)
    {
        for (x = 0; x < width; x++)
        {
            pict[y * linesize + x * 3 + 0] = r;
            pict[y * linesize + x * 3 + 1] = g;
            pict[y * linesize + x * 3 + 2] = b;
        }
    }

    /* draw a vertical lines */
    for (y = 0; y < height; y++)
    {
        for (x = width/3; x < width/3 + 4; x++)
        {
            pict[y * linesize + x * 3 + 0] = 255;
            pict[y * linesize + x * 3 + 1] = y % 255;
            pict[y * linesize + x * 3 + 2] = y % 255;

            pict[y * linesize + (x + 12) * 3 + 0] = 255;
            pict[y * linesize + (x + 12) * 3 + 1] = 255;
            pict[y * linesize + (x + 12) * 3 + 2] = y % 255;
        }
    }
}

#define DBG_WIDTH 320*2
#define DBG_HEGHT 200*2
void main()
{
    Recorder *rec;
    uint8_t *buf = malloc(DBG_WIDTH * DBG_WIDTH * 3);

    recorder_init(&rec, "foo.mov", DBG_WIDTH, DBG_HEGHT, 1, 30000);
    printf("rec %p\n"
           "rec->output_ctx %p\n",
            rec, rec->output_ctx);

    for (int i = 0; i < 200; i += 1)
    {
        int r, g, b;
        r = g = b = i;
        fill_rgb_image(buf, DBG_WIDTH, DBG_HEGHT, r, g, b);
        if (recorder_encode_frame(rec, buf, i*1001) != 0)
        {
            exit(-1);
        }
    }

    recorder_close(rec);
}
#endif