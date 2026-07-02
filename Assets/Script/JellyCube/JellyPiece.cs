using System.Collections.Generic;
using UnityEngine;

public sealed class JellyPiece : MonoBehaviour {

    readonly List<JellyCube> jellies = new List<JellyCube>();

    public int SpawnSlotIndex { get; private set; }
    public IReadOnlyList<JellyCube> Jellies => jellies;
    public bool IsPlaced { get; private set; }

    public void Initialize(int spawnSlotIndex, IReadOnlyList<JellyCube> pieceJellies) {
        SpawnSlotIndex = spawnSlotIndex;
        IsPlaced = false;
        jellies.Clear();

        if (pieceJellies == null) {
            return;
        }

        for (int i = 0; i < pieceJellies.Count; i++) {
            JellyCube jelly = pieceJellies[i];

            if (jelly != null && !jellies.Contains(jelly)) {
                jellies.Add(jelly);
            }
        }
    }

    public void MarkPlaced(Transform placedParent) {
        IsPlaced = true;

        for (int i = 0; i < jellies.Count; i++) {
            JellyCube jelly = jellies[i];

            if (jelly == null) {
                continue;
            }

            Controller controller = jelly.GetComponent<Controller>();

            if (controller != null) {
                controller.ReleaseJelly();
                controller.enabled = false;
            }

            jelly.transform.SetParent(placedParent, true);
        }

        Destroy(gameObject);
    }
}
