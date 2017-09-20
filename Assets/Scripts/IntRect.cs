
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Custom Rect for Dungeon cell operations to avoid constant typecasting
/// </summary>
[System.Serializable] // Exposes struct to Unity
public struct IntRect
{
    public int x, z, width, depth;

    public IntRect(int x, int z, int width, int depth)
    {
        this.x = x;
        this.z = z;
        this.width = width;
        this.depth = depth;
    }

    public bool Contains(IntVector2 point)
    {
        return x <= point.x && point.x <= x + width &&
            z <= point.z && point.z <= z + depth;
    }
}
