# Simple Tower Defense (Unity)

A Unity C# portfolio project. Place towers, defeat enemies, earn currency, upgrade, and survive waves.

**Tech:** Unity Engine, C#, URP, Input System, TextMesh Pro

**Setup & Play:**
1. Clone/download repo & open `TowerDefence` project in Unity Hub.
2. Open scene `Assets/Scenes/SampleScene.unity`.
3. Press Play.
4. **Controls:** Click UI buttons to select towers, click green tiles to build. Click placed towers to upgrade/sell. Right-click deselects.

## Editor Tools
These tools help streamline development and content creation:

- **Level Designer:**
    - **Access:** `Tools > Level Designer` menu item.
    - **Function:** Allows for quick generation of random level layouts.
    - **Usage:** Select a `LevelData` ScriptableObject asset in your Project window. Open the Level Designer tool window. Click the "Generate Random Layout" button. This will modify the selected `LevelData` asset, creating a new grid layout with a guaranteed path for enemies.
- **Tower Icon Generator:**
    - **Access:** `Tools > Tower Defense > Generate Missing Tower Icons` menu item.
    - **Function:** Automatically creates UI icons for towers based on their 3D models.
    - **Usage:** Run the tool. It scans all `TowerData` ScriptableObjects in the project. If a `TowerData` asset has a `towerPrefab` assigned but its `towerIcon` field is empty, the tool generates a preview image of the prefab and saves it as a Sprite asset, assigning it to the `towerIcon` field.
