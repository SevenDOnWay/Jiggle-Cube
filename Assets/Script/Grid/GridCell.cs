using System;
using System.Collections.Generic;
using UnityEngine;

namespace JiggleCube.Gameplay {

    [Serializable]
    public sealed class GridCell {

        [SerializeField] Vector2Int position;
        [SerializeField] GridCellState state;
        [SerializeField] List<JellyCube> cubes;

        public GridCell(Vector2Int position, GridCellState state = GridCellState.Playable) {
            this.position = position;
            this.state = state;
            cubes = new List<JellyCube>();
        }

        public Vector2Int Position => position;
        public GridCellState State => state;
        public bool IsPlayable => state == GridCellState.Playable;
        public bool HasJellies => cubes.Count > 0;
        public bool CanAcceptJelly => IsPlayable && !HasJellies;
        public List<JellyCube> Cubes => cubes;

        public void SetBlocked(bool isBlocked) {
            state = isBlocked ? GridCellState.Blocked : GridCellState.Playable;

            if (isBlocked) {
                cubes.Clear();
            }
        }

        public void SetJellies(IEnumerable<JellyCube> jellies) {
            if (!CanAcceptJelly) {
                throw new InvalidOperationException($"Cannot place jelly on cell {position}.");
            }

            cubes.Clear();

            foreach (JellyCube jelly in jellies) {
                if (jelly == null) {
                    continue;
                }

                jelly.cellPos = position;
                cubes.Add(jelly);
            }
        }

        public bool RemoveJelly(JellyCube jelly) {
            return jelly != null && cubes.Remove(jelly);
        }

        public void Clear() {
            cubes.Clear();
        }
    }
}
