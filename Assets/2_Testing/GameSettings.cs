using DNExtensions.Utilities;
using UnityEngine;

[UniqueSO]
[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    public string gameName = "My Game";
    public int maxPlayers = 4;
}