using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.CarIntro.Editor
{
    public static class CarIntroEnding
    {
        [MenuItem("Hakaton/Интро в машине/Музыкальный финал и прибытие")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните сцену.");
            if(SceneManager.GetActiveScene().name!="CarIntro")EditorSceneManager.OpenScene(CarIntroBuilder.ScenePath);
            var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();Configure(s);EditorUtility.SetDirty(s);EditorUtility.SetDirty(s.picture);EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();return "31.5s: continuous Deception + 4.5s fade; continuous final crane; rain bridge";
        }
        public static void Configure(CarIntroSequence s)
        {
            const string score="Assets/Demo/Audio/Dan Wilkie - Deception.mp3";const string rain="Assets/Demo/Resources/Audio/Rain soft loop.wav";
            s.picture.music.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(score);if(s.picture.music.clip==null)throw new InvalidOperationException("Run Tools/build_car_intro_audio.py first.");
            s.picture.musicFadeIn=0;s.picture.musicFadeOut=4.5f;s.picture.musicVolume=.74f;
            s.cameraShots[5].duration=CarIntroSequence.TitleAccent-s.cameraShots[5].start;s.cameraShots[6].start=CarIntroSequence.TitleAccent;s.cameraShots[6].duration=CarIntroSequence.Length-CarIntroSequence.TitleAccent;
            for(int i=5;i<7;i++){s.picture.shots[i].start=s.cameraShots[i].start;s.picture.shots[i].duration=s.cameraShots[i].duration;}
            if(s.ambience==null){var g=new GameObject("Дождь · звуковой мост к дому");g.transform.SetParent(s.transform,false);s.ambience=g.AddComponent<AudioSource>();}
            s.ambience.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(rain);s.ambience.playOnAwake=false;s.ambience.loop=true;s.ambience.spatialBlend=0;s.ambience.volume=CarIntroSequence.RainStartVolume;
        }
    }
}
