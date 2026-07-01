using System.Collections.Generic;
using UnityEngine;

namespace JiggleCube.Gameplay {

    public sealed class JellyPlacementResult {

        public JellyPlacementResult(List<Vector2Int> placedPositions, List<JellyConnection> connections) {
            PlacedPositions = placedPositions;
            Connections = connections;
        }

        public List<Vector2Int> PlacedPositions { get; }
        public List<JellyConnection> Connections { get; }
        public bool HasConnection => Connections.Count > 0;
    }
}
