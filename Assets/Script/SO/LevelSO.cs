using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelSO", menuName = "Scriptable Objects/LevelSO")]
public class LevelSO : ScriptableObject
{
    public List<Vector2Int> blockedCells = new List<Vector2Int>();

    public bool IsBlocked(Vector2Int position) {
        return blockedCells != null && blockedCells.Contains(position);
    }
}
