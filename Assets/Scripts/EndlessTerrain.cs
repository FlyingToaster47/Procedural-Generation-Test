using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    [SerializeField] private LODInfo[] levelDetails;
    private static float maxViewDist;

    [SerializeField] private Transform viewer;
    [SerializeField] private Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();
        
        maxViewDist = levelDetails[levelDetails.Length-1].lodStep;

        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViewDist/chunkSize);
        UpdateVisibleChunks();

    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            UpdateVisibleChunks();
            viewerPositionOld = viewerPosition;
        }
    }

    void UpdateVisibleChunks() {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/chunkSize);


        for (int i=0;i<terrainChunksVisibleLastUpdate.Count;i++) {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++) {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                } else {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, levelDetails, transform, mapMaterial));
                }
            }

        }
    }


    public class TerrainChunk {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] levelDetails;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataRecieved;
        int prevLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] levelDetails, Transform parent, Material material) {
            this.levelDetails = levelDetails;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionv3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();

            meshObject.transform.position = positionv3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[levelDetails.Length];
            for (int i = 0;i < levelDetails.Length;i++) {
                lodMeshes[i] = new LODMesh(levelDetails[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        } 

        public void UpdateTerrainChunk() {
            if (mapDataRecieved) {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool visible = viewerDistanceFromNearestEdge <= maxViewDist;

                if (visible) {
                    int lodIndex = 0;
                    for (int i = 0;i < levelDetails.Length-1;i++) {
                        if (viewerDistanceFromNearestEdge > levelDetails[i].lodStep) {
                            lodIndex = i+1;
                        } else {
                            break;
                        }
                    }
                    if (lodIndex != prevLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {
                            prevLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        } else if (!lodMesh.hasRequestedMesh){
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);

                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }

        void OnMapDataRecieved(MapData mapData) {
            this.mapData = mapData;
            mapDataRecieved = true;

            Texture2D texture = mapGenerator.GenerateTexture(mapData.colorMap);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }
    }

    class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataRecieved(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }

    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float lodStep;
    }

}
