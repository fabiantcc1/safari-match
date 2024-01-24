using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public GameObject titleObject;

    public float cameraSizeOffset;
    public float cameraVerticalOffset;

    public GameObject[] availablePieces;

    private Tile[,] Tiles;
    private Piece[,] Pieces;

    private Tile startTile;
    private Tile endTile;

    private bool swappingPieces = false;

    private void Start()
    {
        Tiles = new Tile[width, height];
        Pieces = new Piece[width, height];

        SetupBoard();
        PositionCamera();
        SetupPieces();
    }

    private void SetupPieces()
    {
        int maxInteration = 10;
        int currentIteration;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                currentIteration = 0;
                CreatePieceAt(x, y);
                while (HasPreviousMatches(x, y))
                {
                    ClearPieceAt(x, y);
                    CreatePieceAt(x, y);
                    currentIteration++;

                    if (currentIteration > maxInteration)
                    {
                        break;
                    }
                }
            }
        }
    }

    private void ClearPieceAt(int x, int y)
    {
        var pieceToClear = Pieces[x, y];
        Destroy(pieceToClear.gameObject);
        pieceToClear = null;
    }

    private Piece CreatePieceAt(int x, int y)
    {
        var selectedPiece = availablePieces[UnityEngine.Random.Range(0, availablePieces.Length)];
        var o = Instantiate(selectedPiece, new Vector3(x, y, -5), Quaternion.identity);
        o.transform.parent = transform;
        Pieces[x, y] = o.GetComponent<Piece>();
        Pieces[x, y]?.Setup(x, y, this);

        return Pieces[x, y];
    }

    private void PositionCamera()
    {
        float newPostX = (float)width / 2f;
        float newPostY = (float)height / 2f;

        Camera.main.transform.position = new Vector3(newPostX - 0.5f, newPostY - 0.5f + cameraVerticalOffset, -10);

        float horizontal = width + 1f;
        float vertical = (height / 2) + 1f;

        Camera.main.orthographicSize = horizontal > vertical ? horizontal + cameraSizeOffset : vertical + cameraSizeOffset;
    }

    private void SetupBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var o = Instantiate(titleObject, new Vector3(x, y, -5), Quaternion.identity);
                o.transform.parent = transform;
                Tiles[x, y] = o.GetComponent<Tile>();
                Tiles[x, y]?.Setup(x, y, this);
            }
        }
    }

    public void TileDown(Tile _tile)
    {
        startTile = _tile;
    }

    public void TileOver(Tile _tile)
    {
        endTile = _tile;
    }

    public void TileUp(Tile _tile)
    {
        if (startTile != null && endTile != null && IsCloseTo(startTile, endTile))
        {
            StartCoroutine(SwapTile());
        }
    }

    private IEnumerator SwapTile()
    {
        var startPiece = Pieces[startTile.x, startTile.y];
        var endPiece = Pieces[endTile.x, endTile.y];

        startPiece.Move(endTile.x, endTile.y);
        endPiece.Move(startTile.x, startTile.y);

        Pieces[startTile.x, startTile.y] = endPiece;
        Pieces[endTile.x, endTile.y] = startPiece;

        yield return new WaitForSeconds(0.6f);

        bool foundMach = false;
        var startMatches = GetMatchByPiece(startTile.x, startTile.y, 3);
        var endMatches = GetMatchByPiece(endTile.x, endTile.y, 3);

        startMatches.ForEach (piece => {
            foundMach = true;
            ClearPieceAt(piece.x, piece.y);
        });

        endMatches.ForEach(piece => {
            foundMach = true;
            ClearPieceAt(piece.x, piece.y);
        });

        if (!foundMach)
        {
            startPiece.Move(startTile.x, startTile.y);
            endPiece.Move(endTile.x, endTile.y);
            Pieces[startTile.x, startTile.y] = startPiece;
            Pieces[endTile.x, endTile.y] = endPiece;
        }

        startTile = null;
        endTile = null;
        swappingPieces = false;

        yield return null;
    }

    public bool IsCloseTo(Tile start, Tile end)
    {
        if (Math.Abs((start.x - end.x)) == 1 && start.y == end.y)
        {
            return true;
        }

        if (Math.Abs(start.y - end.y) == 1 && start.x == end.x)
        {
            return true;
        }

        return false;
    }

    public List<Piece> GetMatchByDirection(int xpos, int ypos, Vector2 direction, int minPieces = 3)
    {
        List<Piece> matches = new List<Piece>();
        Piece startPiece = Pieces[xpos, ypos];
        matches.Add(startPiece);

        int nextX;
        int nextY;
        int maxVal = width > height ? width : height;

        for (int i = 1; i < maxVal; i++)
        {
            nextX = xpos + ((int)direction.x * i);
            nextY = ypos + ((int)direction.y * i);

            if (nextX >= 0 && nextX < width && nextY >= 0 && nextY < height)
            {
                var nextPiece = Pieces[nextX, nextY];
                if (nextPiece != null && nextPiece.pieceType == startPiece.pieceType)
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
                }
            }
        }

        if (matches.Count >= minPieces)
        {
            return matches;
        }

        return null;
    }

    private bool HasPreviousMatches(int xpos, int ypos)
    {
        var downMatch = GetMatchByDirection(xpos, ypos, Vector2.down, 2) ?? new List<Piece>();
        var leftMatch = GetMatchByDirection(xpos, ypos, Vector2.left, 2) ?? new List<Piece>();

        return (downMatch.Count > 0 || leftMatch.Count > 0);
    }

    public List<Piece> GetMatchByPiece(int xpos, int ypos, int minPieces = 3)
    {
        var upMatch = GetMatchByDirection(xpos, ypos, Vector2.up, 2) ?? new List<Piece>();
        var downMatch = GetMatchByDirection(xpos, ypos, Vector2.down, 2) ?? new List<Piece>();
        var leftMatch = GetMatchByDirection(xpos, ypos, Vector2.left, 2) ?? new List<Piece>();
        var rightMatch = GetMatchByDirection(xpos, ypos, Vector2.right, 2) ?? new List<Piece>();

        var verticalMatches = upMatch.Union(downMatch).ToList();
        var horizontalMatches = leftMatch.Union(rightMatch).ToList();

        var foundMatches = new List<Piece>();

        if (verticalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(verticalMatches).ToList();
        }

        if (horizontalMatches.Count >= minPieces)
        {
            foundMatches = foundMatches.Union(horizontalMatches).ToList();
        }

        return foundMatches;
    }
}
