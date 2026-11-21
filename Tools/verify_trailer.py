#!/usr/bin/env python3
"""Decode the movie and compare its complete soundtrack with the supplied source."""
import argparse
import json
from pathlib import Path
import re
import subprocess
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('--video', type=Path, required=True)
parser.add_argument('--export-info', type=Path, required=True)
parser.add_argument('--ffmpeg', required=True)
parser.add_argument('--report', type=Path, default=Path('Docs/Trailer/video-verification.json'))
args = parser.parse_args()
info = json.loads(args.export_info.read_text())
source = Path(info['music'])
assert 'Tyler Bates' in source.name and 'Choice' in source.name, source
result = subprocess.run([args.ffmpeg, '-hide_banner', '-v', 'error', '-i', str(args.video),
                         '-f', 'null', '-', '-progress', 'pipe:1', '-nostats'],
                        capture_output=True, text=True, check=True)
frames = int(re.findall(r'^frame=(\d+)', result.stdout, re.M)[-1])
# A trim between frame timestamps may discard the final fractional video frame.
assert abs(frames / info['fps'] - info['duration']) <= 1 / info['fps'], (frames, info['duration'])


def audio(path):
    result = subprocess.run([args.ffmpeg, '-v', 'error', '-i', str(path), '-map', '0:a:0',
                             '-ac', '1', '-ar', '8000', '-f', 'f32le', 'pipe:1'],
                            capture_output=True, check=True)
    return np.frombuffer(result.stdout, dtype=np.float32)


original = audio(source)
encoded = audio(args.video)
assert abs(len(original) - len(encoded)) / 8000 < .1, (len(original), len(encoded))
n = min(len(original), len(encoded))
correlation = float(np.corrcoef(original[:n], encoded[:n])[0, 1])
assert correlation > .98, correlation
metadata = subprocess.run([args.ffmpeg, '-hide_banner', '-i', str(args.video)],
                          capture_output=True, text=True).stderr
assert '1280x720' in metadata and '24 fps' in metadata and 'Audio: aac' in metadata, metadata
report = {'file': str(args.video.resolve()), 'music': str(source.resolve()),
          'source_audio_seconds': len(original) / 8000, 'export_audio_seconds': len(encoded) / 8000,
          'timeline_seconds': info['duration'], 'video_seconds': frames / info['fps'],
          'rendered_frames': info['frames'], 'decoded_frames': frames,
          'fps': info['fps'], 'resolution': '1280x720', 'full_music_correlation': correlation,
          'bytes': args.video.stat().st_size, 'decode_errors': result.stderr.strip()}
args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2))
print(json.dumps(report, ensure_ascii=False, indent=2))
