using System.Collections.Generic;
using UnityEngine;



public static class JellyConnectionFinder {

    /*
    public static List<JellyConnection> FindAffected( JellyGrid grid, IReadOnlyList<Vector2Int> changedPositions, int minimumConnectionSize = 3 ) {
        List<JellyConnection> connections = new List<JellyConnection>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        if ( grid == null || changedPositions == null ) {
            return connections;
        }

        for ( int i = 0; i < changedPositions.Count; i++ ) {
            Vector2Int start = changedPositions[i];

            if ( !grid.IsInside(start) || visited.Contains(start) ) {
                continue;
            }

            JellyColor? color = grid.GetColor(start);

            if ( !color.HasValue ) {
                visited.Add(start);
                continue;
            }

            List<Vector2Int> group = FloodFillSameColor(grid, start, color.Value);

            for ( int groupIndex = 0; groupIndex < group.Count; groupIndex++ ) {
                visited.Add(group[groupIndex]);
            }

            if ( group.Count >= minimumConnectionSize ) {
                connections.Add(new JellyConnection(color.Value, group));
            }
        }

        return connections;
    }

    public static List<JellyConnection> FindAll( JellyGrid grid, int minimumConnectionSize = 3 ) {
        List<JellyConnection> connections = new List<JellyConnection>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        if ( grid == null ) {
            return connections;
        }

        for ( int x = 0; x < grid.Width; x++ ) {
            for ( int y = 0; y < grid.Height; y++ ) {
                Vector2Int start = new Vector2Int(x, y);

                if ( visited.Contains(start) ) {
                    continue;
                }

                JellyColor? color = grid.GetColor(start);

                if ( !color.HasValue ) {
                    visited.Add(start);
                    continue;
                }

                List<Vector2Int> group = FloodFillSameColor(grid, start, color.Value);

                for ( int i = 0; i < group.Count; i++ ) {
                    visited.Add(group[i]);
                }

                if ( group.Count >= minimumConnectionSize ) {
                    connections.Add(new JellyConnection(color.Value, group));
                }
            }
        }

        return connections;
    }

    static List<Vector2Int> FloodFillSameColor( JellyGrid grid, Vector2Int start, JellyColor color ) {
        List<Vector2Int> group = new List<Vector2Int>();
        Queue<Vector2Int> open = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        open.Enqueue(start);
        visited.Add(start);

        while ( open.Count > 0 ) {
            Vector2Int current = open.Dequeue();
            JellyColor? currentColor = grid.GetColor(current);

            if ( !currentColor.HasValue || currentColor.Value != color ) {
                continue;
            }

            group.Add(current);

            foreach ( Vector2Int neighbor in grid.GetNeighbors4(current) ) {
                if ( visited.Contains(neighbor) ) {
                    continue;
                }

                JellyColor? neighborColor = grid.GetColor(neighbor);

                if ( neighborColor.HasValue && neighborColor.Value == color ) {
                    visited.Add(neighbor);
                    open.Enqueue(neighbor);
                }
            }
        }

        return group;
    }

    */
}

