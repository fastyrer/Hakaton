using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using Hakaton.Demo.CarIntro;
public static class VerifyCarIntroHandoff
{
    public static async Task<string> Main()
    {
        var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();if(!Application.isPlaying||s==null)throw new Exception("Start CarIntro");
        var suspended=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Mouse)).ToArray();foreach(var d in suspended)InputSystem.DisableDevice(d);
        var keyboard=InputSystem.AddDevice<Keyboard>("Dialogue handoff check");keyboard.MakeCurrent();var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Action start=null;bool fired=false;
        try
        {
            s.externalClock=false;s.HoldForMonologue();start=()=>{if(keyboard.spaceKey.wasPressedThisFrame){fired=true;s.BeginAfterMonologue();InputSystem.onAfterUpdate-=start;}};InputSystem.onAfterUpdate+=start;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
            double began=EditorApplication.timeSinceStartup;var done=new TaskCompletionSource<bool>();EditorApplication.CallbackFunction tick=null;tick=()=>{if(EditorApplication.timeSinceStartup-began>.25){EditorApplication.update-=tick;done.TrySetResult(true);}};EditorApplication.update+=tick;await done.Task;
            if(!fired||!s.IsPlaying||s.Paused||s.CurrentTime<.05f)throw new Exception("Last dialogue Space paused the cinematic");
            string message="PASS  Space that closes manual dialogue starts the cinematic without immediately pausing it";File.WriteAllText("Docs/CarIntro/handoff-verification.txt",message+"\n");return message;
        }
        finally{s.externalClock=true;s.picture.music.Stop();InputSystem.onAfterUpdate-=start;InputSystem.RemoveDevice(keyboard);foreach(var d in suspended)if(d.added)InputSystem.EnableDevice(d);InputSystem.settings.backgroundBehavior=background;}
    }
}
