using System;
using System.Collections;
using System.Collections.Generic;
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
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var selectedPiece = availablePieces[UnityEngine.Random.Range(0, availablePieces.Length)];
                var o = Instantiate(selectedPiece, new Vector3(x, y, -5), Quaternion.identity);
                o.transform.parent = transform;
                Pieces[x, y] = o.GetComponent<Piece>();
                Pieces[x, y]?.Setup(x, y, this);
            }
        }
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
            SwapTile();
        }

        startTile = null;
        endTile = null;
    }

    private void SwapTile()
    {
        var startPiece = Pieces[startTile.x, startTile.y];
        var endPiece = Pieces[endTile.x, endTile.y];

        startPiece.Move(endTile.x, endTile.y);
        endPiece.Move(startTile.x, startTile.y);

        Pieces[startTile.x, startTile.y] = endPiece;
        Pieces[endTile.x, endTile.y] = startPiece;
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
}
