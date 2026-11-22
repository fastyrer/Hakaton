#!/usr/bin/env python3
"""Prepare a quiet recorded CC0 rain bed. Deception plays continuously from 00:00.
Music uses the original MP3 and Unity's 4.5-second volume fade; there is no music edit.
"""
import argparse,hashlib,json,subprocess
from pathlib import Path
import numpy as np
import soundfile as sf
p=argparse.ArgumentParser();p.add_argument('--ffmpeg',required=True);a=p.parse_args()
source=Path('Assets/Demo/Audio/Dan Wilkie - Deception.mp3');recording=Path('Assets/ThirdParty/YlmirRain/4.ogg');sr=44100
raw=subprocess.run([a.ffmpeg,'-v','error','-i',str(recording),'-ac','2','-ar',str(sr),'-f','f32le','pipe:1'],capture_output=True,check=True).stdout
x=np.frombuffer(raw,np.float32).reshape(-1,2);freq=np.fft.rfftfreq(len(x),1/sr)
# Periodic zero-phase EQ: remove rumble and sharp upper hiss without a short modulation loop.
highpass=1/np.sqrt(1+(120/np.maximum(freq,.001))**4);lowpass=1/np.sqrt(1+(freq/4500)**8);rain=np.fft.irfft(np.fft.rfft(x,axis=0)*(highpass*lowpass)[:,None],n=len(x),axis=0)
rain*=.035/np.sqrt(np.mean(rain**2));assert abs(rain).max()<.9
folder=Path('Assets/Demo/Resources/Audio');folder.mkdir(parents=True,exist_ok=True);path=folder/'Rain soft loop.wav';sf.write(path,rain,sr,subtype='PCM_16')
report={'source':str(source),'source_start_seconds':0,'duration_seconds':31.5,'music':str(source),'music_volume':.74,'music_fade_out_seconds':4.5,'music_fade_start_seconds':27,'music_splices':[],'time_stretch':False,'ambience':str(path),'rain_source':str(recording),'rain_author':'Ylmir','rain_license':'CC0-1.0','rain_source_url':'https://opengameart.org/content/rain-loopable','rain_source_sha256':hashlib.sha256(recording.read_bytes()).hexdigest(),'rain_loop_seconds':len(rain)/sr,'rain_asset_rms':float(np.sqrt(np.mean(rain**2))),'rain_start_volume':.02,'rain_end_volume':.07,'rain_rise_start':24.124,'rain_rise_end':29.2,'maximum_rain_output_rms':float(np.sqrt(np.mean(rain**2))*.07)}
Path('Docs/CarIntro/music-edit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
