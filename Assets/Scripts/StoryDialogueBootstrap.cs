using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns the story subtitle beats for the father's house at scene load,
/// so dialogues stay in one place instead of hand-placed scene objects.
/// Positions are world coordinates of the FatherHouse hallway/yard.
/// </summary>
public static class StoryDialogueBootstrap
{
    // SubsystemRegistration runs on every play (even with domain reload disabled),
    // unlike AfterSceneLoad which fires only for the very first scene load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "FatherHouse") return;
        if (Object.FindAnyObjectByType<FatherHouseSubtitleTrigger>() != null) return;
        var root = new GameObject("Сюжетные реплики");
        SceneManager.MoveGameObjectToScene(root, scene);

        // 2 · Звонок напарника — по пути от машины к крыльцу.
        root.AddComponent<FatherHouseSubtitleTrigger>()
            .AsZone(new Vector3(0f, 0f, -3f), 3f)
            .WithGuard("story_partner_call");

        // 3 · Вход в дом — первые шаги в прихожей.
        root.AddComponent<HouseEntranceSubtitleTrigger>()
            .AsZone(new Vector3(0f, 0f, 2.2f), 1.8f)
            .WithGuard("story_entrance");

        // Заколоченное окно в прихожей.
        root.AddComponent<SubtitleSequence>()
            .AsZone(new Vector3(-3.2f, 0f, 0.6f), 2f)
            .WithGuard("story_window")
            .WithLines(new SubtitleSequence.Line { time = 0f, text = "А: Даже это заколотил. Ну и зачем тебе тогда окна вообще?" });

        // Запасы на кухне.
        root.AddComponent<SubtitleSequence>()
            .AsZone(new Vector3(10.5f, 0f, 13f), 3f)
            .WithGuard("story_kitchen")
            .WithLines(new SubtitleSequence.Line { time = 0f, text = "А: Ты тут войну собирался пережидать?" });

        // Реплика при первом осмотре семейной фотографии (улика 0).
        root.AddComponent<SubtitleSequence>()
            .AsEvent("evidence_0")
            .WithLines(new SubtitleSequence.Line { time = 0f, text = "А: О, мы все здесь. Надо забрать." });
    }
}
