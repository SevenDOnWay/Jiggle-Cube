using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JellyShapeSO", menuName = "Scriptable Objects/Jelly Shape")]
public class JellyShapeSO : ScriptableObject {

    public string shapeId = "Shape";
    public int slotResolution = 2;
    public List<JellyPartShapeData> parts = new List<JellyPartShapeData>();

    public List<string> Validate() {
        List<string> errors = new List<string>();

        if (slotResolution <= 0) {
            errors.Add($"{name} has invalid slot resolution {slotResolution}.");
        }

        if (parts == null || parts.Count == 0) {
            errors.Add($"{name} must contain at least one jelly part.");
            return errors;
        }

        for (int partIndex = 0; partIndex < parts.Count; partIndex++) {
            JellyPartShapeData part = parts[partIndex];

            if (part == null) {
                errors.Add($"{name} part {partIndex} is null.");
                continue;
            }

            if (part.occupiedSlots == null || part.occupiedSlots.Count == 0) {
                errors.Add($"{name} part {partIndex} must occupy at least one slot.");
                continue;
            }

            for (int slotIndex = 0; slotIndex < part.occupiedSlots.Count; slotIndex++) {
                Vector2Int slot = part.occupiedSlots[slotIndex];

                if (slot.x < 0 || slot.y < 0 || slot.x >= slotResolution || slot.y >= slotResolution) {
                    errors.Add($"{name} part {partIndex} slot {slot} is outside resolution {slotResolution}.");
                }
            }
        }

        return errors;
    }
}

[Serializable]
public sealed class JellyPartShapeData {

    public List<Vector2Int> occupiedSlots = new List<Vector2Int>();
}
