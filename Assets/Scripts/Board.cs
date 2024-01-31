using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public float timeBetweenPieces = 0.05f;

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
        StartCoroutine(SetupPieces());
    }

    private void Update()
    {
        Debug.Log(swappingPieces);
    }

    private IEnumerator SetupPieces()
    {
        int maxInteration = 50;
        int currentIteration;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                yield return new WaitForSeconds(timeBetweenPieces);

                if (Pieces[x, y] == null)
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

        yield return null;
    }

    private void ClearPieceAt(int x, int y)
    {
        Destroy(Pieces[x, y].gameObject);
        Pieces[x, y] = null;
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
        if (swappingPieces) return;
        startTile = _tile;
    }

    public void TileOver(Tile _tile)
    {
        if (swappingPieces) return;
        endTile = _tile;
    }

    public void TileUp(Tile _tile)
    {
        if (swappingPieces) return;

        if (startTile != null && endTile != null && IsCloseTo(startTile, endTile))
        {
            StartCoroutine(SwapTile());
        }
    }

    private IEnumerator SwapTile()
    {
        swappingPieces = true;

        var startPiece = Pieces[startTile.x, startTile.y];
        var endPiece = Pieces[endTile.x, endTile.y];

        startPiece.Move(endTile.x, endTile.y);
        endPiece.Move(startTile.x, startTile.y);

        Pieces[startTile.x, startTile.y] = endPiece;
        Pieces[endTile.x, endTile.y] = startPiece;

        yield return new WaitForSeconds(0.6f);

        var startMatches = GetMatchByPiece(startTile.x, startTile.y, 3);
        var endMatches = GetMatchByPiece(endTile.x, endTile.y, 3);

        var allMatches = startMatches.Union(endMatches).ToList();

        

        if (allMatches.Count == 0)
        {
            startPiece.Move(startTile.x, startTile.y);
            endPiece.Move(endTile.x, endTile.y);
            Pieces[startTile.x, startTile.y] = startPiece;
            Pieces[endTile.x, endTile.y] = endPiece;

            swappingPieces = false;
        }
        else
        {
            ClearPieces(allMatches);
        }

        startTile = null;
        endTile = null;

        yield return null;
    }

    private void ClearPieces(List<Piece> piecesToClear)
    {
        piecesToClear.ForEach(piece => {
            ClearPieceAt(piece.x, piece.y);
        });

        List<int> columns = GetColumns(piecesToClear);
        List<Piece> collapsedPieces = CollapaseColumns(columns, 0.3f);

        FindMatchesRecursively(collapsedPieces);
    }

    private void FindMatchesRecursively(List<Piece> collapsedPieces)
    {
        StartCoroutine(FindMatchesRecursivelyCoroutine(collapsedPieces));
    }

    private IEnumerator FindMatchesRecursivelyCoroutine(List<Piece> collapsedPieces)
    {
        yield return new WaitForSeconds(1f);

        List<Piece> newMatches = new List<Piece>();

        collapsedPieces.ForEach(piece => {
            var matches = GetMatchByPiece(piece.x, piece.y, 3);

            if (matches != null)
            {
                newMatches = newMatches.Union(matches).ToList();
                ClearPieces(matches);
            }
        });

        if (newMatches.Count > 0)
        {
            var newCollapsedPieces = CollapaseColumns(GetColumns(newMatches), 0.3f);
            FindMatchesRecursively(newCollapsedPieces);
        }
        else
        {
            yield return new WaitForSeconds(0.1f);

            yield return StartCoroutine(SetupPieces());
            swappingPieces = false;
        }

        yield return null;
    }

    private List<Piece> CollapaseColumns(List<int> columns, float timeToCollapse)
    {
        List<Piece> movingPieces = new List<Piece>();

        for (int x = 0; x < columns.Count; x++)
        {
            var column = columns[x];

            for (int y = 0; y < height; y++)
            {
                if (Pieces[column, y] == null)
                {
                    for (int yplus = y + 1; yplus < height; yplus++)
                    {
                        if (Pieces[column, yplus] != null)
                        {
                            Pieces[column, yplus].Move(column, y);
                            Pieces[column, y] = Pieces[column, yplus];

                            if (!movingPieces.Contains(Pieces[column, y])) movingPieces.Add(Pieces[column, y]);

                            Pieces[column, yplus] = null;
                            break;
                        }
                    }
                }
            }
        }

        return movingPieces;
    }

    private List<int> GetColumns(List<Piece> piecesToClear)
    {
        var result = new List<int>();

        piecesToClear.ForEach(piece => {
            if (!result.Contains(piece.x))
            {
                result.Add(piece.x);
            }
        });

        return result;
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
