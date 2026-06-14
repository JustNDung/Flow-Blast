using Core;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Editor utility to create item and box prefabs for the conveyor belt system.
    /// Usage: Right-click in Project window -> Create -> Flow Blast -> [Item Prefab | Box Prefab]
    /// </summary>
    public static class ItemPrefabCreator
    {
        [MenuItem("Assets/Create/Flow Blast/Item Prefab", priority = 1)]
        private static void CreateItemPrefab()
        {
            GameObject prefab = new GameObject("Item_Default", typeof(SpriteRenderer), typeof(ItemPrefab));
            
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.sortingOrder = 1;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Item Prefab",
                "ItemPrefab",
                "prefab",
                "Choose where to save the item prefab");

            if (!string.IsNullOrEmpty(path))
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Object.DestroyImmediate(prefab);
                AssetDatabase.Refresh();
                Debug.Log($"[ItemPrefabCreator] Created item prefab at: {path}");
            }
            else
            {
                Object.DestroyImmediate(prefab);
            }
        }

        [MenuItem("Assets/Create/Flow Blast/Box Prefab", priority = 2)]
        private static void CreateBoxPrefab()
        {
            // BoxPrefab requires the script component for LevelSpawner to use it properly
            GameObject prefab = new GameObject("Box_Default", typeof(SpriteRenderer), typeof(BoxPrefab));
            
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.sortingOrder = 1;

            // Set a default color for the box (slightly larger than item visually)
            BoxPrefab boxComponent = prefab.GetComponent<BoxPrefab>();
            if (boxComponent != null)
            {
                boxComponent.SpriteRenderer.color = Color.white;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Box Prefab",
                "BoxPrefab",
                "prefab",
                "Choose where to save the box prefab");

            if (!string.IsNullOrEmpty(path))
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Object.DestroyImmediate(prefab);
                AssetDatabase.Refresh();
                Debug.Log($"[ItemPrefabCreator] Created box prefab at: {path}");
            }
            else
            {
                Object.DestroyImmediate(prefab);
            }
        }
    }
}