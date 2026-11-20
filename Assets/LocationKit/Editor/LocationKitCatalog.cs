using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hakaton.LocationKit.Editor
{
    public sealed class LocationKitCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Item
        {
            public string id;
            public string title;
            public string description;
            public KitCategory category;
            public GameObject prefab;
            public bool preferSurface;
            public Bounds bounds;
        }
        [Serializable]
        public sealed class Finish { public string title; public Material material; }
        public List<Item> items = new List<Item>();
        public List<Finish> finishes = new List<Finish>();
        public GameObject playerRig;
        public Item Find(string id) => items.Find(i => i.id == id);
    }
}
