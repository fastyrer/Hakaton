using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hakaton.Demo.Editor
{
    public static class BoardSceneSetup
    {
        // Upgrades the existing editable scene without regenerating the house or yard.
        [MenuItem("Hakaton/Enable inventory and detective board")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            if (game == null) throw new InvalidOperationException("Open HouseArrival or HouseDemo first.");
            Configure(game);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static void Configure(DemoGame game)
        {
            game.board = game.GetComponent<DetectiveBoard>() ?? game.gameObject.AddComponent<DetectiveBoard>();
            var items = new DemoInteractable[DemoGame.EvidenceTitles.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var obj = GameObject.Find(DemoGame.EvidenceTitles[i]);
                if (obj == null) throw new InvalidOperationException("Evidence prop is missing: " + DemoGame.EvidenceTitles[i]);
                var item = obj.GetComponent<DemoInteractable>() ?? obj.AddComponent<DemoInteractable>();
                item.kind = DemoInteractable.Interaction.Evidence;
                item.evidenceId = i;
                item.label = "Взять / осмотреть · " + DemoGame.EvidenceTitles[i];
                item.enabled = true;
                var surface = obj.transform.Find("Readable surface");
                if (surface != null) item.illustration = surface.GetComponent<Renderer>().sharedMaterial.mainTexture as Texture2D;
                items[i] = item;
                EditorUtility.SetDirty(item);
            }
            game.evidence = items;
            EditorUtility.SetDirty(game);
        }
    }
}
