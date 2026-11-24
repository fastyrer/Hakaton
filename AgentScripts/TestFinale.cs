using System;
using System.Threading.Tasks;
using Hakaton.Demo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TestFinale
{
    static async Task<DemoGame> Game()
    {
        DemoGame game = null;
        while (game == null || game.hud == null)
        {
            game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            if (game == null || game.hud == null) await Task.Delay(500);
        }
        return game;
    }

    static void Click(string prefix)
    {
        foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
        {
            var text = button.GetComponentInChildren<Text>();
            if (text == null || !text.text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            button.onClick.Invoke();
            Debug.Log("CLICK " + prefix + " :: " + text.text.Split('\n')[0]);
            return;
        }
        Debug.Log("CLICK MISS " + prefix);
    }

    static void Log(string message) => Debug.Log("FINALE " + message);

    public static async Task<int> Truth()
    {
        var game = await Game();
        await Task.Delay(500);
        var note = UnityEngine.Object.FindAnyObjectByType<FinalNote>();
        Log("note=" + (note != null));
        if (note == null) return -1;
        note.Interact(game);
        Log("readEv=" + InvestigationTravel.GetEventState(EvidenceCatalog.EvFinaleRead) + " mode=" + game.hud.Mode);
        await Task.Delay(1000);
        Click("ДАЛЕЕ"); // закрыть записку → монолог
        await Task.Delay(32000);
        Log("afterMonolog mode=" + game.hud.Mode + " (ожидание Ending)");
        Click("ПРИНЯТЬ ПРАВДУ");
        await Task.Delay(1200);
        Log("narration mode=" + game.hud.Mode + " truth=" + InvestigationTravel.GetEventState(EvidenceCatalog.EvEndingTruth)
            + " illusion=" + InvestigationTravel.GetEventState(EvidenceCatalog.EvEndingIllusion));
        Click("ДАЛЕЕ");
        await Task.Delay(5000); // затемнение
        Log("news mode=" + game.hud.Mode);
        Click("ДАЛЕЕ");
        await Task.Delay(1200);
        Log("art mode=" + game.hud.Mode);
        Click("В ГЛАВНОЕ МЕНЮ");
        await Task.Delay(3500);
        Log("scene=" + SceneManager.GetActiveScene().name);
        return 0;
    }

    public static async Task<int> Illusion()
    {
        var game = await Game();
        await Task.Delay(500);
        var note = UnityEngine.Object.FindAnyObjectByType<FinalNote>();
        Log("note=" + (note != null));
        if (note == null) return -1;
        note.Interact(game);
        await Task.Delay(1000);
        Click("ДАЛЕЕ");
        await Task.Delay(32000);
        Log("afterMonolog mode=" + game.hud.Mode + " (ожидание Ending)");
        Click("ОТКАЗАТЬСЯ ОТ ПРАВДЫ");
        await Task.Delay(1200);
        Log("narration mode=" + game.hud.Mode + " truth=" + InvestigationTravel.GetEventState(EvidenceCatalog.EvEndingTruth)
            + " illusion=" + InvestigationTravel.GetEventState(EvidenceCatalog.EvEndingIllusion));
        Click("ДАЛЕЕ");
        await Task.Delay(5000);
        Log("news mode=" + game.hud.Mode);
        Click("ДАЛЕЕ");
        await Task.Delay(1200);
        Log("art mode=" + game.hud.Mode);
        Click("В ГЛАВНОЕ МЕНЮ");
        await Task.Delay(3500);
        Log("scene=" + SceneManager.GetActiveScene().name);
        return 0;
    }
}
