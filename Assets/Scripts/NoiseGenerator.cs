using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseGenerator {

    public static float[,] GenerateNoiseMap(int width, int height, float scale, int octaves, float persistance, float lacunarity, int seed, Vector2 offset) {
        float[,] noiseMap = new float[width,height];
        for (int x=0;x<width;x++) {
            for (int y=0;y<height;y++) {
                float noiseHeight = 0;
                float amplitude = 1f;
                float frequency = 1f;
                for (int i=0;i<octaves;i++) {
                    float xCoord = (float)(x + offset.x)/ scale * frequency;
                    float yCoord = (float)(y + offset.y)/ scale * frequency;

                    float samplePoint = Mathf.PerlinNoise(xCoord + seed, yCoord + seed);
                    noiseHeight += samplePoint * amplitude;
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        return noiseMap;
    }

}
