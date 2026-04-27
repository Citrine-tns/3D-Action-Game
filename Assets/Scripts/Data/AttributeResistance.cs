using System;
using UnityEngine;

/// <summary>
/// 属性耐性。敵やオブジェクトにアタッチして使う。
/// 倍率が低いほど硬い。1.0 = 標準、0.5 = 耐性、1.5 = 弱点。
/// </summary>
[Serializable]
public struct AttributeResistance
{
    [Range(0f, 3f)] public float slash;
    [Range(0f, 3f)] public float crush;
    [Range(0f, 3f)] public float pierce;

    public static AttributeResistance Default => new()
    {
        slash = 1f,
        crush = 1f,
        pierce = 1f
    };

    public float GetMultiplier(PhysicalAttribute attribute)
    {
        return attribute switch
        {
            PhysicalAttribute.Slash => slash,
            PhysicalAttribute.Crush => crush,
            PhysicalAttribute.Pierce => pierce,
            _ => 1f
        };
    }
}
