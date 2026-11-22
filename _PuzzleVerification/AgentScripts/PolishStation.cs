using System.Linq;
using Hakaton.Demo;
using Hakaton.Demo.Editor;
using Hakaton.LocationKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PolishStation
{
    public static object Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="PoliceStation")throw new System.Exception("Open station in edit mode");
        var archive=GameObject.Find("04 · Архив · дело отца В-97/041").transform;
        var chair=archive.Cast<Transform>().First(x=>x.name=="chair");
        Undo.RecordObject(chair,"Доступ к архивным документам");
        chair.SetPositionAndRotation(new Vector3(7,0,28.3f),Quaternion.Euler(0,180,0));PrefabUtility.RecordPrefabInstancePropertyModifications(chair);
        foreach(var clue in archive.GetComponentsInChildren<DemoInteractable>().Where(x=>x.kind==DemoInteractable.Interaction.Evidence))
        {
            var label=clue.transform.Find("Надпись на деле");
            if(label==null)continue;
            label.SetParent(archive,false);label.position=clue.transform.position+Vector3.up*.022f;
            label.rotation=Quaternion.Euler(90,0,0);label.localScale=Vector3.one;
        }
        var track=PoliceStationBuilder.FindTrack();
        if(track==null)throw new System.Exception("Audio import not ready");
        string path=AssetDatabase.GetAssetPath(track);
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);
        var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;
        importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        var phones=Object.FindAnyObjectByType<ArrivalHeadphones>();phones.track=track;
        EditorUtility.SetDirty(phones);
        Hakaton.LocationKit.Editor.LocationKitOperations.RefreshConnections(Object.FindAnyObjectByType<LocationRoot>());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return new {track=track.name,length=track.length,scene=SceneManager.GetActiveScene().path};
    }
}
