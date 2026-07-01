using System;
using UnityEngine;

namespace JiggleCube.Gameplay {

    [Serializable]
    public readonly struct JellyShapeCell {

        public JellyShapeCell(Vector2Int offset, JellyColor color) {
            Offset = offset;
            Color = color;
        }

        public Vector2Int Offset { get; }
        public JellyColor Color { get; }
    }
}
