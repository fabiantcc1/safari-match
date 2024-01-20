using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;
    public Board board;

    public void Setup(int _x, int _y, Board _board)
    {
        x = _x;
        y = _y;
        board = _board;
    }
}
