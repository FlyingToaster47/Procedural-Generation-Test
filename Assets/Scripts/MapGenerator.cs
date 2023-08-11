using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

using UnityEngine;

public class MapGenerator : MonoBehaviour {
    [SerializeField] private Vector2 offset = Vector2.zero;

    [SerializeField] private float scale;
    [SerializeField] private float persistance;
    [SerializeField] private float lacunarity;
    [SerializeField] private int octaves;
    [SerializeField] private int seed;

    [SerializeField] private TerrainColor[] terrainColors;
    [SerializeField] private Renderer textureRenderer;

    [SerializeField] private float heightMultiplier;
    [SerializeField] private AnimationCurve heightCurve;

    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    public const int mapChunkSize = 241;
    [Range(0, 6)]
    [SerializeField] private int editorLOD;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void Generate() {
        MapData mapData = GenerateMapData(Vector2.zero);
        Texture2D texture = GenerateTexture(mapData.colorMap);
        textureRenderer.sharedMaterial.mainTexture = texture;

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, heightMultiplier, heightCurve, editorLOD);

        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;

    }

    MapData GenerateMapData(Vector2 centre) {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(mapChunkSize, mapChunkSize, scale, octaves, persistance, lacunarity, seed, centre + offset);
        Color[] colorMap = GenerateColorMap(noiseMap);

        return new MapData(noiseMap, colorMap);
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }


    void MapDataThread(Vector2 centre, Action<MapData> callback) {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }


    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, heightMultiplier, heightCurve, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update() {
        Generate();
        if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0;i < mapDataThreadInfoQueue.Count;i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback (threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0;i < meshDataThreadInfoQueue.Count;i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback (threadInfo.parameter);
            }
        }
    }

    private Color[] GenerateColorMap(float[,] noiseMap) {
        Color[] colorMap = new Color[mapChunkSize*mapChunkSize];
        for (int x=0;x<mapChunkSize;x++) {
            for (int y=0;y<mapChunkSize;y++) {
                float samplePoint = noiseMap[x, y];
                for (int i=0;i<terrainColors.Length;i++) {
                    if (samplePoint >= terrainColors[i].height) {
                        colorMap[y*mapChunkSize+x] = terrainColors[i].color;
                    }
                }

                //colorMap[y*mapChunkSize+x] = new Color(samplePoint, samplePoint, samplePoint);
            }
        }
        return colorMap;
    }

    public Texture2D GenerateTexture(Color[] colorMap) {
        Texture2D mapTexture;
        mapTexture = new Texture2D(mapChunkSize, mapChunkSize);
        mapTexture.SetPixels(colorMap);
        mapTexture.filterMode = FilterMode.Point;
        mapTexture.Apply();
        return mapTexture;
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }


}

[System.Serializable]
struct TerrainColor {
    public Color color;
    public float height;
}

public struct MapData {
    public readonly float [,] noiseMap;
    public readonly Color[] colorMap;

    public MapData (float[,] noiseMap, Color[] colorMap) {
        this.noiseMap = noiseMap;
        this.colorMap = colorMap;
    }
}