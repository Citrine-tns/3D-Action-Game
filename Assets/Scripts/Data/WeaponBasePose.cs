using System;
using UnityEngine;

/// <summary>
/// 武器装備時の基本姿勢と歩行パラメータ。
/// WeaponData に埋め込んで Inspector で設定する。
/// </summary>
[Serializable]
public struct WeaponBasePose
{
    [Header("体幹")]
    [Tooltip("Body Pivot（背骨の付け根）の回転。前傾など")]
    public Vector3 body;

    [Header("左腕")]
    public Vector3 armL1;
    public Vector3 armL2;

    [Header("右腕")]
    public Vector3 armR1;
    public Vector3 armR2;

    [Header("左脚")]
    public Vector3 legL1;
    public Vector3 legL2;

    [Header("右脚")]
    public Vector3 legR1;
    public Vector3 legR2;

    [Header("手首（武器ローカル回転）")]
    public Vector3 wrist;

    [Header("歩行時の振り幅（0=構え固定, 1=通常振り）")]
    [Range(0f, 1f)] public float armLSwing;
    [Range(0f, 1f)] public float armRSwing;
    [Range(0f, 1f)] public float legSwing;
}
