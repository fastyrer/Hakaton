using System.Collections.Generic;
using UnityEngine;

namespace Hakaton.Demo
{
    public static class InvestigationTravel
    {
        static int[] found;
        static List<DetectiveBoard.Entry> entries;
        static List<DetectiveBoard.Connection> connections;
        static readonly Dictionary<int, Texture2D> images = new Dictionary<int, Texture2D>();
        static readonly Dictionary<string, PuzzleState> puzzles = new Dictionary<string, PuzzleState>();
        static readonly HashSet<string> items = new HashSet<string>();
        // Story events (TZ: not_ready / ready / triggered, fired only once per playthrough).
        static readonly Dictionary<string, int> events = new Dictionary<string, int>();
        // Story progress shared across scenes: the study safe unlocks the Ivan/Dmitry
        // travel destinations; placed figurines reappear when the house reloads.
        public static bool SafeDiscovered;
        public static readonly HashSet<string> PlacedFigurines = new HashSet<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            found = null; entries = null; connections = null; images.Clear(); puzzles.Clear(); items.Clear();
            events.Clear(); SafeDiscovered = false; PlacedFigurines.Clear();
        }
        public static StoryEventState GetEventState(string id) =>
            !string.IsNullOrEmpty(id) && events.TryGetValue(id, out var state) ? (StoryEventState)state : StoryEventState.NotReady;
        public static void SetEventState(string id, StoryEventState state)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (GetEventState(id) < state) events[id] = (int)state;
        }
        public static Texture2D Illustration(int id) => images.TryGetValue(id, out var image) ? image : null;
        public static void Capture(DemoGame game)
        {
            found = new int[game.Found.Count]; game.Found.CopyTo(found);
            entries = new List<DetectiveBoard.Entry>();
            foreach (var entry in game.board.Entries) entries.Add(entry.Copy());
            connections = new List<DetectiveBoard.Connection>();
            foreach (var connection in game.board.Connections)
                connections.Add(new DetectiveBoard.Connection { a = connection.a, b = connection.b, confirmed = connection.confirmed });
            puzzles.Clear(); items.Clear();
            foreach (var pair in game.PuzzleStates) puzzles[pair.Key] = pair.Value;
            foreach (string item in game.Items) items.Add(item);
            foreach (int id in found)
            { var image = game.EvidenceIllustration(id); if (image != null) images[id] = image; }
        }
        public static void Restore(DemoGame game)
        {
            if (found == null) return;
            foreach (int id in found) game.Found.Add(id);
            game.board.RestoreEntries(entries);
            game.board.RestoreConnections(connections);
            game.PuzzleStates.Clear(); game.Items.Clear();
            foreach (var pair in puzzles) game.PuzzleStates[pair.Key] = pair.Value;
            foreach (string item in items) game.Items.Add(item);
            found = null; entries = null; connections = null; puzzles.Clear(); items.Clear();
        }
    }
}
