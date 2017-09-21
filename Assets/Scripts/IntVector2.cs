
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Custom Vector2 for Dungeon cell operations
/// </summary>
[System.Serializable] // Exposes struct to Unity
public struct IntVector2
{
    public int x, z;

    public IntVector2(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static IntVector2 Zero
    {
        get
        {
            return new IntVector2(0, 0);
        }
    }

    public static IntVector2 North
    {
        get
        {
            return new IntVector2(0, 1);
        }
    }

    public static IntVector2 South
    {
        get
        {
            return new IntVector2(0, -1);
        }
    }

    public static IntVector2 East
    {
        get
        {
            return new IntVector2(1, 0);
        }
    }

    public static IntVector2 West
    {
        get
        {
            return new IntVector2(-1, 0);
        }
    }

    /// <summary>
    /// Array of cardinal direction normals
    /// </summary>
    public static IntVector2[] Cardinals =
    {
        North,
        South,
        East,
        West
    };

    /// <summary>
    /// Array of intercardinal direction normals
    /// </summary>
    public static IntVector2[] Intercardinals =
    {
        North + East,
        North + West,
        South + East,
        South + West
    };

    /// <summary>
    /// Return a random cardinal direction normal
    /// </summary>
    public static IntVector2 RandomCardinal
    {
        get
        {
            return Cardinals[Random.Range(0, 3)];
        }
    }

    public static IntVector2 operator + (IntVector2 a, IntVector2 b)
    {
        a.x += b.x;
        a.z += b.z;
        return a;
    }

    public static IntVector2 operator * (IntVector2 a, int b)
    {
        a.x *= b;
        a.z *= b;
        return a;
    }
}
