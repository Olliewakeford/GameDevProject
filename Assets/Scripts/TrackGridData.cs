using UnityEngine;

[CreateAssetMenu(fileName = "TrackGridData", menuName = "Track/TrackGridData")]
public class TrackGridData : ScriptableObject
{
    public int resolution;
    public bool[] grid; // Flattened 2D array: grid[x + z * resolution]

    public bool IsOnTrack(int x, int z)
    {
        if (x < 0 || z < 0 || x >= resolution || z >= resolution) return false;
        return grid[x + z * resolution];
    }
}
