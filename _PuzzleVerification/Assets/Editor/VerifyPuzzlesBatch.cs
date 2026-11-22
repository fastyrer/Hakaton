using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerifyPuzzlesBatch
{
    public static async void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/FatherHouse.unity");
            EditorApplication.EnterPlaymode();
            while (!Application.isPlaying) await Task.Delay(100);
            await Task.Delay(500);
            var results = await VerifyPuzzles.Main();
            Debug.Log("PUZZLE VERIFICATION PASS: " + results.Length);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("PUZZLE VERIFICATION FAIL: " + e);
            EditorApplication.Exit(1);
        }
    }
}
