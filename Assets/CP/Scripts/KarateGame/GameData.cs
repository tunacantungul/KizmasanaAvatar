// In file: Assets/CP/Scripts/KarateGame/GameData.cs
// This is a simple static class to hold data between scenes.
// From your other scene, before loading the Karate Game, you would set:
// GameData.NpcElement = Element.Fire; // or Water, or Earth
// GameData.IsInitialized = true;

public static class GameData
{
    public static bool IsInitialized = false;
    public static Element NpcElement;
}
