#!/usr/bin/env python3
"""Combine native Unity review frames into a labelled contact sheet."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--frames', type=Path, default=Path('Docs/Trailer'))
parser.add_argument('--output', type=Path, default=Path('Docs/Trailer/contact-sheet-franks-choice.jpg'))
parser.add_argument('--edit', type=Path, default=Path('Assets/Demo/Trailer/Art/PreservedStationEdit.json'))
args = parser.parse_args()
shots = json.loads(args.edit.read_text())['shots']
files = sorted(args.frames.glob('shot-??.jpg'))
assert len(files) == len(shots), (len(files), len(shots))
width, height, label, columns = 384, 216, 23, 5
sheet = Image.new('RGB', (columns * width, ((len(files) + columns - 1) // columns) * (height + label)), '#101211')
draw = ImageDraw.Draw(sheet)
font_path = Path('/System/Library/Fonts/Supplemental/Arial.ttf')
font = ImageFont.truetype(str(font_path), 12) if font_path.exists() else ImageFont.load_default()
for i, file in enumerate(files):
    x, y = (i % columns) * width, (i // columns) * (height + label)
    with Image.open(file) as frame:
        sheet.paste(frame.resize((width, height), Image.Resampling.LANCZOS), (x, y))
    seconds = round(shots[i]['start'])
    draw.text((x + 7, y + height + 4), f'{i:02}  {seconds//60:02}:{seconds%60:02}  {shots[i]["label"]}', fill='#d6cbb7', font=font)
args.output.parent.mkdir(parents=True, exist_ok=True)
sheet.save(args.output, quality=93)
print(args.output)
