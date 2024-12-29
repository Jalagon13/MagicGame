using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
public struct Tile
{
    public Tile(float4 s)
    {
        color = s;
        //isLightSource = isLight;
    }
    public float4 color;
    //public bool isLightSource;
    //public string name;
    //public int lightStrength;
    //public float2 position;
}

public class World : MonoBehaviour
{
    public GameObject player;
    public GameObject lightmap;
    public Tilemap tilemap;
    public GameObject pallet;
    public TileBase lightTile;
    public TileBase wallTile;
    public ComputeShader computeShader;
    public RenderTexture renderTexture;
    public SpriteRenderer spriteRenderer;
    public RawImage rawImage;
    Dictionary<Vector2Int, TileBase> allTiles;
    TileBase[] loadedTiles;
    Tile[] computeShaderTiles;

    const int WORLD_WIDTH = 104;
    const int WORLD_HEIGHT = 104;

    Vector2Int playerQuad;
    List<Vector2Int> loadedChunks;
    TileBase tile;
    private void Start()
    {
        tile = pallet.GetComponentInChildren<Tilemap>().GetTile(new Vector3Int(0, 0, 0));
        
        renderTexture = new RenderTexture(WORLD_WIDTH*4, WORLD_HEIGHT*4, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;                       //AXCTUALLY IMPORTANT
        renderTexture.Create();

        playerQuad = new Vector2Int(tilemap.LocalToCell(player.transform.position).x / 8, tilemap.LocalToCell(player.transform.position).y / 8);
        loadedChunks = new List<Vector2Int>();
        for (int x = -2; x < 3; x++)
        {
            for (int y = -2; y < 3; y++)
            {
                loadedChunks.Add(new Vector2Int(playerQuad.x + x, playerQuad.y + y));
            }
        }
        allTiles = new Dictionary<Vector2Int, TileBase>();
        for (int x = 0; x < 200; x++)
        {
            for (int y = 0; y < 200; y++)
            {
                allTiles[new Vector2Int(x, y)] = tile;
            }
        }

        UpdateEntireTileBuffer();
        //ComputeGPU();
    }

    public void Update()
    {
        if (tilemap.LocalToCell(player.transform.position).x / 8 != playerQuad.x ||
            tilemap.LocalToCell(player.transform.position).y / 8 != playerQuad.y) //Player is in a new quad
        {
            int count = 0;
            Vector2Int newQuad = new Vector2Int(tilemap.LocalToCell(player.transform.position).x / 8, tilemap.LocalToCell(player.transform.position).y / 8);
            //Debug.Log(newQuad);           
            List<Vector2Int> newChunks = new List<Vector2Int>();
            for (int x2 = -6; x2 < 7; x2++)
            {
                for (int y2 = -6; y2 < 7; y2++)
                {
                    newChunks.Add(new Vector2Int(newQuad.x + x2, newQuad.y + y2));
                    if (!loadedChunks.Contains(new Vector2Int(newQuad.x + x2, newQuad.y + y2))) //Loading a new chunk
                    {
                        count++;
                        for (int x = 0; x < 8; x++)
                        {
                            for (int y = 0; y < 8; y++)
                            {   
                                Vector3Int pos = new Vector3Int(x + ((newQuad.x + x2) * 8), y + ((newQuad.y + y2) * 8));
                                if (allTiles.ContainsKey(new Vector2Int(pos.x, pos.y)))
                                {
                                    tilemap.SetTile(pos, allTiles[new Vector2Int(pos.x, pos.y)]);
                                }
                                
                                //tilemap.SetTile(pos, allTiles[new Vector2Int(pos.x, pos.y)]);
                                //tilemap.SetTile(new Vector3Int(x + 8 * 6 + (playerQuad.x * 8), y - 8 * 6, 0), tile2);
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < loadedChunks.Count; i++) //Unloading chunks
            {
                if (!newChunks.Contains(loadedChunks[i]))
                {
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            tilemap.SetTile(new Vector3Int(loadedChunks[i].x * 8 + x, loadedChunks[i].y * 8 + y), null);
                        }
                    }
                }
            }
            playerQuad = new Vector2Int(newQuad.x, newQuad.y);
            loadedChunks = newChunks;

            Vector3 currentLocation = lightmap.transform.position;
            currentLocation.x = playerQuad.x * 8;
            currentLocation.y = playerQuad.y * 8;
            lightmap.transform.position = currentLocation;
            UpdateEntireTileBuffer();
        }

        Vector3 point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int pointInt = new Vector3Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y), 0);
        if (Input.GetMouseButton(0))
        {
            tilemap.SetTile(pointInt, null);
            Vector3Int localPos = tilemap.LocalToCell(pointInt);
            int key = (localPos.x - (playerQuad.x - 6) * 8) * 4 + ((localPos.y - (playerQuad.y - 6) * 8) * WORLD_WIDTH * 16);
            //computeShaderTiles[key] = new Tile(new Vector4(0.0f,0.0f,0.0f,0.0f));
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    computeShaderTiles[key+x+(416*y)] = new Tile(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                }
            }
        }
        if (Input.GetMouseButton(1))
        {
            tilemap.SetTile(pointInt, lightTile);
            Vector3Int localPos = tilemap.LocalToCell(pointInt);
            //Debug.Log(localPos.x - (playerQuad.x - 6) * 8 + ((103 - (localPos.y - (playerQuad.y - 6) * 8)) * WORLD_WIDTH));
            int key = (localPos.x - (playerQuad.x - 6) * 8) * 4 + ((localPos.y - (playerQuad.y - 6) * 8) * WORLD_WIDTH * 16);
            //Debug.Log(key);
            //computeShaderTiles[key] = new Tile(new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    computeShaderTiles[key+x+(416*y)] = new Tile(new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                }
            }
            //ComputeGPU();
        }
                if (Input.GetKeyDown(KeyCode.E))
        {
            tilemap.SetTile(pointInt, lightTile);
            Vector3Int localPos = tilemap.LocalToCell(pointInt);
            //Debug.Log(localPos.x - (playerQuad.x - 6) * 8 + ((103 - (localPos.y - (playerQuad.y - 6) * 8)) * WORLD_WIDTH));
            int key = (localPos.x - (playerQuad.x - 6) * 8) * 4 + ((localPos.y - (playerQuad.y - 6) * 8) * WORLD_WIDTH * 16);
            //Debug.Log(key);
            //computeShaderTiles[key] = new Tile(new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    computeShaderTiles[key+x+(416*y)] = new Tile(new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                }
            }
            //ComputeGPU();
        }
                        if (Input.GetKeyDown(KeyCode.W))
        {
            tilemap.SetTile(pointInt, lightTile);
            Vector3Int localPos = tilemap.LocalToCell(pointInt);
            //Debug.Log(localPos.x - (playerQuad.x - 6) * 8 + ((103 - (localPos.y - (playerQuad.y - 6) * 8)) * WORLD_WIDTH));
            int key = (localPos.x - (playerQuad.x - 6) * 8) * 4 + ((localPos.y - (playerQuad.y - 6) * 8) * WORLD_WIDTH * 16);
            //Debug.Log(key);
            //computeShaderTiles[key] = new Tile(new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    computeShaderTiles[key+x+(416*y)] = new Tile(new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                }
            }
            //ComputeGPU();
        }
                        if (Input.GetKeyDown(KeyCode.Q))
        {
            tilemap.SetTile(pointInt, lightTile);
            Vector3Int localPos = tilemap.LocalToCell(pointInt);
            //Debug.Log(localPos.x - (playerQuad.x - 6) * 8 + ((103 - (localPos.y - (playerQuad.y - 6) * 8)) * WORLD_WIDTH));
            int key = (localPos.x - (playerQuad.x - 6) * 8) * 4 + ((localPos.y - (playerQuad.y - 6) * 8) * WORLD_WIDTH * 16);
            //Debug.Log(key);
            //computeShaderTiles[key] = new Tile(new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    computeShaderTiles[key+x+(416*y)] = new Tile(new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
                }
            }
            //ComputeGPU();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log(playerQuad);
        }

        ComputeGPU();
    }

    public void UpdateEntireTileBuffer()
    {
        loadedTiles = tilemap.GetTilesBlock(new BoundsInt(new Vector3Int(playerQuad.x * 8 - 48, playerQuad.y * 8 - 48, 0), new Vector3Int(104, 104, 1)));

        computeShaderTiles = new Tile[loadedTiles.Length*32];

        for (int i = 0; i < loadedTiles.Length; i++)
        {
            Tile tile;
            int key = (i % 104) * 4;
            key = key + (i / 104) * 1664;
            if (loadedTiles[i] == null)
            {
                tile = new Tile(new float4(0.0f, 0.0f, 0.0f, 0.0f));
            }
            else
            {
                switch (loadedTiles[i].name)
                {
                    case "Stone": 
                        tile = new Tile(new float4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case "Sand":
                        tile = new Tile(new float4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;

                    default:
                        tile = new Tile(new float4(0.0f, 0.0f, 0.0f, 0.0f));
                        break;
                }
            }
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    computeShaderTiles[key+x+(416*y)] = tile;
                }
            }
        }
    }

    public void ComputeGPU()
    {
        ComputeBuffer tilesBuffer = new ComputeBuffer(computeShaderTiles.Length, sizeof(float) * 4);

        tilesBuffer.SetData(computeShaderTiles);

        computeShader.SetBuffer(0, "tiles", tilesBuffer);
        computeShader.SetTexture(0, "Result", renderTexture);

        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        tilesBuffer.GetData(computeShaderTiles);

        Texture displayImage = renderTexture;
        rawImage.texture = displayImage;
        tilesBuffer.Dispose();
    }
}