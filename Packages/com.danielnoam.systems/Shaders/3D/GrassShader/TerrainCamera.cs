using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;

public class TerrainCamera : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Terrain[] terrains;

    [InfoBox("After updating, set _Terrain_Offset (X,Z) and _Terrain_Size on the grass material manually.")]
    [SerializeField] private Vector2 terrainOffset;
    [SerializeField] private float terrainSize;

    [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
    private void FindAllTerrains()
    {
        terrains = FindObjectsByType<Terrain>();
    }

    [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
    private void UpdatePosition()
    {
        if (!cam || terrains.Length == 0) return;

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (var t in terrains)
        {
            Vector3 pos = t.transform.position;
            Vector3 size = t.terrainData.size;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos + size);
        }

        Vector3 totalSize = max - min;
        Vector3 center = (min + max) / 2f;

        cam.transform.position = new Vector3(center.x, max.y + 350f, center.z);
        cam.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        cam.orthographicSize = Mathf.Max(totalSize.x, totalSize.z) / 2f;

        terrainOffset = new Vector2(min.x, min.z);
        terrainSize = Mathf.Max(totalSize.x, totalSize.z);
    }
}