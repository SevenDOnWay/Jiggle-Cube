using System;
using System.Collections.Generic;
using UnityEngine;



public sealed class JellyGrid {

    /*
    static readonly Vector2Int[] NeighborOffsets = {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

    readonly GridCell[,] cells;

    public JellyGrid( int width, int height ) {
        if ( width <= 0 ) {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid width must be greater than zero.");
        }

        if ( height <= 0 ) {
            throw new ArgumentOutOfRangeException(nameof(height), "Grid height must be greater than zero.");
        }

        Width = width;
        Height = height;
        cells = new GridCell[width, height];

        for ( int x = 0; x < width; x++ ) {
            for ( int y = 0; y < height; y++ ) {
                cells[x, y] = new GridCell(new Vector2Int(x, y));
            }
        }
    }

    public int Width { get; }
    public int Height { get; }

    public bool IsInside( Vector2Int position ) {
        return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height;
    }

    public GridCell GetCell( Vector2Int position ) {
        EnsureInside(position);
        return cells[position.x, position.y];
    }

    public JellyColor? GetColor( Vector2Int position ) {
        return GetCell(position).Color;
    }

    public void SetBlocked( Vector2Int position, bool blocked ) {
        EnsureInside(position);
        cells[position.x, position.y].SetBlocked(blocked);
    }

    public void SetColor( Vector2Int position, JellyColor color ) {
        EnsureInside(position);
        cells[position.x, position.y].SetColor(color);
    }

    public bool CanPlace( Vector2Int anchor, IReadOnlyList<JellyShapeCell> shapeCells ) {
        if ( shapeCells == null || shapeCells.Count == 0 ) {
            return false;
        }

        for ( int i = 0; i < shapeCells.Count; i++ ) {
            Vector2Int position = anchor + shapeCells[i].Offset;

            if ( !IsInside(position) || !cells[position.x, position.y].CanAcceptJelly ) {
                return false;
            }
        }

        return true;
    }

    public JellyPlacementResult Place( Vector2Int anchor, IReadOnlyList<JellyShapeCell> shapeCells, int minimumConnectionSize = 3 ) {
        if ( !CanPlace(anchor, shapeCells) ) {
            throw new InvalidOperationException($"Shape cannot be placed at anchor {anchor}.");
        }

        List<Vector2Int> placedPositions = new List<Vector2Int>(shapeCells.Count);

        for ( int i = 0; i < shapeCells.Count; i++ ) {
            JellyShapeCell shapeCell = shapeCells[i];
            Vector2Int position = anchor + shapeCell.Offset;
            cells[position.x, position.y].SetColor(shapeCell.Color);
            placedPositions.Add(position);
        }

        List<JellyConnection> connections = JellyConnectionFinder.FindAffected(this, placedPositions, minimumConnectionSize);
        return new JellyPlacementResult(placedPositions, connections);
    }

    public bool TryPlace( Vector2Int anchor, IReadOnlyList<JellyShapeCell> shapeCells, out JellyPlacementResult result, int minimumConnectionSize = 3 ) {
        if ( !CanPlace(anchor, shapeCells) ) {
            result = null;
            return false;
        }

        result = Place(anchor, shapeCells, minimumConnectionSize);
        return true;
    }

    public void Clear( IReadOnlyList<Vector2Int> positions ) {
        if ( positions == null ) {
            return;
        }

        for ( int i = 0; i < positions.Count; i++ ) {
            Vector2Int position = positions[i];

            if ( IsInside(position) ) {
                cells[position.x, position.y].Clear();
            }
        }
    }

    public IEnumerable<Vector2Int> GetNeighbors4( Vector2Int position ) {
        for ( int i = 0; i < NeighborOffsets.Length; i++ ) {
            Vector2Int neighbor = position + NeighborOffsets[i];

            if ( IsInside(neighbor) ) {
                yield return neighbor;
            }
        }
    }

    void EnsureInside( Vector2Int position ) {
        if ( !IsInside(position) ) {
            throw new ArgumentOutOfRangeException(nameof(position), $"Grid position {position} is outside {Width}x{Height}.");
        }
    }

    */


}

