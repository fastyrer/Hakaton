#!/usr/bin/env python3
"""Decode the entire movie and verify the continuous original score, fade and quiet recorded rain."""
import argparse,json,re,subprocess
from pathlib import Path
import numpy as np
p=argparse.ArgumentParser();p.add_argument('--video',type=Path,default=Path('Builds/Trailers/CarIntro-Arrival-Deception-31s.mp4'));p.add_argument('--frames',type=Path,default=Path('/tmp/hakaton-car-intro-frames'));p.add_argument('--ffmpeg',required=True);a=p.parse_args();info=json.loads((a.frames/'complete.json').read_text());duration=info['duration'];assert info['frames']==round(duration*info['fps'])
r=subprocess.run([a.ffmpeg,'-v','error','-i',str(a.video),'-f','null','-','-progress','pipe:1','-nostats'],capture_output=True,check=True);count=int(re.findall(rb'^frame=(\d+)',r.stdout,re.M)[-1]);assert count==info['frames'];assert not r.stderr.strip()
def decode(path,rate=8000,loop=False,limit=True):
 cmd=[a.ffmpeg,'-v','error']+(['-stream_loop','-1'] if loop else [])+['-i',str(path)]+(['-t',str(duration)] if limit else [])+['-map','0:a:0','-ac','1','-ar',str(rate),'-f','f32le','pipe:1'];return np.frombuffer(subprocess.run(cmd,capture_output=True,check=True).stdout,np.float32)
rate=8000;t=np.arange(round(duration*rate))/rate;music=decode(info['music']);expected=music[:len(t)]*info['musicVolume']
if info['musicFadeIn']>0:expected*=np.clip(t/info['musicFadeIn'],0,1)
if info['musicFadeOut']>0:expected*=np.clip((duration-t)/info['musicFadeOut'],0,1)
if info.get('ambience'):
 rain=decode(info['ambience'],loop=True)[:len(t)];u=np.clip((t-info['rainRiseStart'])/(info['rainRiseEnd']-info['rainRiseStart']),0,1);gain=info['rainStartVolume']+(info['rainEndVolume']-info['rainStartVolume'])*u*u*(3-2*u);expected+=rain*gain
actual=decode(a.video);assert abs(len(actual)-len(expected))<=640;n=min(len(actual),len(expected));correlation=float(np.corrcoef(expected[:n],actual[:n])[0,1]);assert correlation>.995,correlation
edit=json.loads(Path('Docs/CarIntro/music-edit.json').read_text());assert edit['music_splices']==[] and not edit['time_stretch'];assert Path(info['music'])==Path(edit['source']);assert info['musicFadeOut']==4.5
# Check the old splice region separately: rendered audio must still follow source seconds 24..31.
late=(t[:n]>=24)&(t[:n]<31);late_corr=float(np.corrcoef(expected[:n][late],actual[:n][late])[0,1]);assert late_corr>.995,late_corr
music_envelope=np.clip((duration-t)/info['musicFadeOut'],0,1);faded_music=music[:len(t)]*info['musicVolume']*music_envelope
end_rms=float(np.sqrt(np.mean(faded_music[-1600:]**2)));assert end_rms<.004
rain_rms=float(np.sqrt(np.mean(rain**2)));maximum_rain_rms=rain_rms*max(info['rainStartVolume'],info['rainEndVolume']);assert maximum_rain_rms<.003
metadata=subprocess.run([a.ffmpeg,'-hide_banner','-i',str(a.video)],capture_output=True,text=True).stderr;assert '1280x720' in metadata and f'{info["fps"]} fps' in metadata and 'Audio: aac' in metadata
report={'file':str(a.video.resolve()),'music':info['music'],'duration_seconds':duration,'frames':count,'fps':info['fps'],'resolution':'1280x720','audio_mix_correlation':correlation,'source_start_seconds':0,'music_splices':[],'music_fade_out_seconds':info['musicFadeOut'],'late_source_continuity_correlation':late_corr,'maximum_rain_output_rms':maximum_rain_rms,'last_200ms_music_rms':end_rms,'decode_errors':'','bytes':a.video.stat().st_size}
Path('Docs/CarIntro/video-verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
