#!/usr/bin/env python3
"""Encode Unity's deterministic master frames and the supplied music into MP4."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--frames', type=Path, required=True)
    parser.add_argument('--music', type=Path)
    parser.add_argument('--output', type=Path, default=Path('Builds/Trailers/OpeningTrailer.mp4'))
    parser.add_argument('--ffmpeg', default=shutil.which('ffmpeg'))
    args = parser.parse_args()
    encoder = args.ffmpeg
    if not encoder:
        try:
            import imageio_ffmpeg
            encoder = imageio_ffmpeg.get_ffmpeg_exe()
        except ImportError:
            parser.error('Install FFmpeg or pass --ffmpeg /path/to/ffmpeg')
    info = json.loads((args.frames / 'complete.json').read_text())
    files = list(args.frames.glob('?????.jpg'))
    if len(files) != info['frames']:
        parser.error(f"Expected {info['frames']} completed frames, found {len(files)}")
    duration = info['duration']
    music = args.music or Path(info['music'])
    if not music.is_file():
        parser.error(f'Music file not found: {music}')
    filters = [f"volume={info.get('musicVolume', .65)}"]
    fade_in, fade_out = info.get('musicFadeIn', 0), info.get('musicFadeOut', 0)
    if fade_in > 0:
        filters.append(f'afade=t=in:st=0:d={fade_in}')
    if fade_out > 0:
        filters.append(f'afade=t=out:st={duration-fade_out:.6f}:d={fade_out}')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    audio_inputs=[]
    audio_map=['-map', '1:a:0', '-af', ','.join(filters)]
    if info.get('ambience'):
        audio_inputs=['-stream_loop', '-1', '-i', info['ambience']]
        start,end=info['rainRiseStart'],info['rainRiseEnd']
        q=f"clip((t-{start})/({end-start}),0,1)"
        gain=f"{info['rainStartVolume']}+({info['rainEndVolume']}-{info['rainStartVolume']})*({q})*({q})*(3-2*({q}))"
        gain=f"if(isnan(t),{info['rainStartVolume']},{gain})"
        graph=f"[1:a]{','.join(filters)}[score];[2:a]volume='{gain}':eval=frame[rain];[score][rain]amix=inputs=2:duration=first:normalize=0[sound]"
        audio_map=['-filter_complex',graph,'-map','[sound]']
    subprocess.run([
        encoder, '-y', '-hide_banner', '-loglevel', 'warning',
        '-framerate', str(info['fps']), '-i', str(args.frames / '%05d.jpg'),
        '-i', str(music), *audio_inputs, '-t', f'{duration:.6f}',
        '-map', '0:v:0', *audio_map,
        '-c:v', 'libx264', '-preset', 'medium', '-crf', '18', '-pix_fmt', 'yuv420p',
        '-color_primaries', 'bt709', '-color_trc', 'bt709', '-colorspace', 'bt709',
        '-c:a', 'aac', '-b:a', '256k',
        '-movflags', '+faststart', '-metadata', 'title=Дом помнит — вступление',
        '-metadata', f'comment=Music: {music.stem}; picture edit {duration:.3f}s',
        str(args.output)
    ], check=True)
    print(json.dumps({'output': str(args.output.resolve()), 'duration': duration,
                      'music': str(music), 'frames': info['frames'], 'fps': info['fps'], 'bytes': args.output.stat().st_size}, ensure_ascii=False))


if __name__ == '__main__':
    main()
