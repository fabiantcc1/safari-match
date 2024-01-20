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

    private void Start()
    {
        SetupBoard();
        PositionCamera();
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
                o.GetComponent<Tile>()?.Setup(x, y, this);
            }
        }
    }
}
