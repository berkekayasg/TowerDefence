using UnityEngine;
using UnityEditor;
using System.IO; // Required for Path operations

public class TowerDataIconGenerator
{
    [MenuItem("Tools/Generate Missing Tower Icons")]
    private static void GenerateMissingTowerIcons()
    {
        string[] guids = AssetDatabase.FindAssets("t:TowerData");
        int generatedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        if (guids.Length == 0)
        {
            Debug.Log("No TowerData assets found in the project.");
            return;
        }

        Debug.Log($"Found {guids.Length} TowerData assets. Checking for missing icons...");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TowerData towerData = AssetDatabase.LoadAssetAtPath<TowerData>(path);

            if (towerData == null)
            {
                Debug.LogWarning($"Could not load TowerData at path: {path}");
                errorCount++;
                continue;
            }

            // Check if prefab exists and icon is missing
            if (towerData.towerPrefab != null && towerData.towerIcon == null)
            {
                Debug.Log($"Generating icon for: {towerData.name} (Prefab: {towerData.towerPrefab.name})");

                Texture2D previewTexture = AssetPreview.GetAssetPreview(towerData.towerPrefab);

                if (previewTexture == null)
                {
                    // Sometimes the preview isn't ready immediately.
                    // A more robust solution might involve waiting or retrying,
                    // but for a manual tool, asking the user to try again is often sufficient.
                    Debug.LogWarning($"Preview for {towerData.towerPrefab.name} not ready yet. Try running the tool again shortly.", towerData);
                    errorCount++;
                    continue; // Skip this one for now
                }

                // Create a new Texture2D from the preview to make it persistent
                // Previews are often temporary internal textures.
                Texture2D persistentTexture = new Texture2D(previewTexture.width, previewTexture.height, previewTexture.format, false);
                Graphics.CopyTexture(previewTexture, persistentTexture);
                persistentTexture.Apply(); // Apply the changes

                // Create a Sprite from the persistent texture
                // Ensure pivot is center (0.5, 0.5) for typical UI use
                Sprite newSprite = Sprite.Create(persistentTexture, new Rect(0, 0, persistentTexture.width, persistentTexture.height), new Vector2(0.5f, 0.5f));
                newSprite.name = $"{towerData.towerPrefab.name}_Icon"; // Give the sprite a meaningful name

                // --- Important: Save the Sprite as an Asset ---
                // Sprites need to be saved as assets themselves to be referenced reliably.
                // We'll save it next to the TowerData asset.
                string spritePath = Path.Combine(Path.GetDirectoryName(path), $"{newSprite.name}.png"); // Save as PNG

                // Encode the texture to PNG bytes
                byte[] pngData = persistentTexture.EncodeToPNG();
                if (pngData != null)
                {
                    File.WriteAllBytes(spritePath, pngData);
                    AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate); // Import the new asset

                    // Configure sprite import settings (optional but good practice)
                    TextureImporter textureImporter = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                    if (textureImporter != null)
                    {
                        textureImporter.textureType = TextureImporterType.Sprite;
                        textureImporter.spriteImportMode = SpriteImportMode.Single;
                        textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
                        textureImporter.mipmapEnabled = false; // Usually not needed for UI icons
                        textureImporter.SaveAndReimport(); // Apply settings
                    }

                    // Load the newly created Sprite asset
                    Sprite savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                    if (savedSprite != null)
                    {
                        // Assign the *saved* sprite asset to the TowerData
                        towerData.towerIcon = savedSprite;
                        EditorUtility.SetDirty(towerData); // Mark the TowerData as changed
                        generatedCount++;
                        Debug.Log($"Successfully generated and assigned icon for {towerData.name} at {spritePath}", towerData);
                    }
                    else
                    {
                         Debug.LogError($"Failed to load the saved sprite at {spritePath}. Icon not assigned for {towerData.name}.", towerData);
                         errorCount++;
                         // Clean up the failed PNG file? Maybe not, user might want to inspect it.
                    }
                }
                else
                {
                    Debug.LogError($"Failed to encode texture to PNG for {towerData.name}. Icon not generated.", towerData);
                    errorCount++;
                }
                 // Clean up the temporary persistent texture if it wasn't used or saved
                 if (persistentTexture != null && towerData.towerIcon == null) // Only destroy if not successfully used
                 {
                     Object.DestroyImmediate(persistentTexture); // DestroyImmediate needed in Editor scripts
                 }
            }
            else if (towerData.towerPrefab == null)
            {
                // Debug.Log($"Skipping {towerData.name}: No tower prefab assigned.");
                skippedCount++;
            }
            else // Icon already exists
            {
                // Debug.Log($"Skipping {towerData.name}: Icon already assigned.");
                skippedCount++;
            }
        }

        if (generatedCount > 0)
        {
            AssetDatabase.SaveAssets(); // Save changes to all modified TowerData assets
            AssetDatabase.Refresh();    // Refresh asset database to reflect changes
            Debug.Log($"Icon generation complete. Generated: {generatedCount}, Skipped: {skippedCount}, Errors/Not Ready: {errorCount}");
        }
        else
        {
            Debug.Log($"Icon generation complete. No new icons generated. Skipped: {skippedCount}, Errors/Not Ready: {errorCount}");
        }
    }
}
