# プレイヤーモデル & アニメーション

## 概要
プレイヤーの体を6パーツのキューブで構成し、移動・戦闘のモーションをコードで制御する

---

## キャラクターモデル（6パーツキューブ）

### オブジェクト階層

```
Player（CharacterController）
├── PlayerModel（空オブジェクト。モデル全体のルート）
│   ├── Head（Cube）
│   ├── Body（Cube。胴体）
│   ├── ArmL_Pivot（空。左肩の位置）
│   │   └── ArmL（Cube）
│   ├── ArmR_Pivot（空。右肩の位置）
│   │   └── ArmR（Cube）
│   ├── LegL_Pivot（空。左腰の位置）
│   │   └── LegL（Cube）
│   └── LegR_Pivot（空。右腰の位置）
│       └── LegR（Cube）
└── WeaponPivot（右手先端あたり）
    └── [武器]（WeaponHolder が生成）
```

### 各パーツのサイズ目安

| パーツ | Scale (X, Y, Z) | Pivot位置の目安 |
|--------|-----------------|----------------|
| Head | 0.4, 0.4, 0.4 | Body の上 |
| Body | 0.5, 0.6, 0.3 | 中心 |
| Arm | 0.2, 0.5, 0.2 | 肩（Cube上端が肩に来るよう Offset Y:-0.25） |
| Leg | 0.25, 0.5, 0.25 | 腰（Cube上端が腰に来るよう Offset Y:-0.25） |

- Pivot は関節位置に置く（回転の原点になる）
- Cube モデルは Pivot の子にして、Offset で位置調整
- Collider は見た目用パーツには不要（CharacterController が担当）

---

## 歩行アニメーション

### 方針
マイクラ式。手足を前後に振るだけの簡易アニメーション

### 挙動
- 体全体は CharacterController による平行移動のみ（上下の揺れなし）
- 動くのは手足だけ。頭・胴体は動かない
- 手足は X 軸回転の Sin 波で前後に振る
- 右手と左足、左手と右足が同位相（対角ペア）
- 停止時: その時点の手足の角度から初期姿勢（角度0）へ Lerp で戻る（即座にリセットしない）

```
  停止時        歩行中（ある瞬間）     停止直後

   [頭]            [頭]                [頭]
   [胴]            [胴]                [胴]
  |    |         ＼    ／              ＼  ／  ← Lerp で
  |    |          |    |               |  |      徐々に戻る
  |    |         ／    ＼              |  ＼
```

### パラメータ

| 項目 | 値（仮） | 説明 |
|------|---------|------|
| swingAngle | 30° | 手足の最大振り角度 |
| swingSpeed | 移動速度に比例 | 歩行サイクルの速さ |
| returnSpeed | 10 | 停止後に初期姿勢へ戻る Lerp 速度 |

### 擬似コード
```
if (isMoving)
{
    float phase = Time.time * swingSpeed;
    float angle = sin(phase) * swingAngle;

    armR_target  = +angle;
    armL_target  = -angle;
    legR_target  = -angle;
    legL_target  = +angle;
}
else
{
    // 停止 → 全パーツ 0° に向かって Lerp
    armR_target = 0;
    armL_target = 0;
    legR_target = 0;
    legL_target = 0;
}

// 現在角度からターゲットへ Lerp（移動中も停止後も同じ補間処理）
armR_current = Lerp(armR_current, armR_target, returnSpeed * dt);
armL_current = Lerp(armL_current, armL_target, returnSpeed * dt);
legR_current = Lerp(legR_current, legR_target, returnSpeed * dt);
legL_current = Lerp(legL_current, legL_target, returnSpeed * dt);

armR_Pivot.localRotation = Euler(armR_current, 0, 0);
armL_Pivot.localRotation = Euler(armL_current, 0, 0);
legR_Pivot.localRotation = Euler(legR_current, 0, 0);
legL_Pivot.localRotation = Euler(legL_current, 0, 0);
```

---

## 戦闘姿勢（コンボ連動）

### AnimationClip はどこまで制御できるか

Unity の AnimationClip はヒエラルキー内の**全 Transform** を制御できる。
つまり武器の位置回転だけでなく、腕・胴・脚の姿勢も含めて1つのクリップで制御可能。

ただし初期段階では AnimationClip なしのコード駆動で進め、
必要に応じてクリップで上書きする設計にする。

### コード駆動の戦闘姿勢

ComboNodeData の startPosition / endPosition に応じて、
武器だけでなくプレイヤーの体パーツにも構えのポーズを設定する。

| NodePosition | 体の姿勢（コード駆動） |
|---|---|
| Upper | 両腕を上に上げる。胴体やや後傾 |
| Lower | 両腕を下に振り下ろす。胴体やや前傾 |
| Left | 胴体を左にひねる。右腕が左側に |
| Right | 胴体を右にひねる。右腕が右側に |
| Front | 突き姿勢。右腕が前方に伸びる |

### 状態遷移とポーズの流れ

```
[Idle]
  体: 初期姿勢（自然な立ち姿）
  武器: 待機位置（WeaponSwing.idleRotation）

    ↓ 攻撃入力

[Active]（motionDuration の間）
  体: ノードの startPosition の構え → endPosition の構えへ補間
  武器: WeaponSwing が startPosition → endPosition を Bezier 補間（既存処理）

    ↓ motionDuration 経過

[Recovery]（inputWindowDuration or recoveryDuration の間）
  体: endPosition の構えで停止（武器も終端位置で停止）

    ├─ 入力あり → 次ノードの [Active] へ
    │   体: 現在の endPosition から次ノードの startPosition → endPosition へ
    │   （前ノードの endPosition = 次ノードの startPosition なので滑らかに繋がる）
    │
    └─ 入力なし or 最終ノード → [Idle] へ
        体: endPosition の構えから初期姿勢へ Lerp で戻る
        武器: 終端位置から待機位置へ Slerp で戻る（既存処理）
```

### AnimationClip による上書き（将来対応）

ComboNodeData に animationClip フィールドが既にある。
clip が設定されている場合はコード駆動ポーズを無視し、clip の内容で体と武器を制御する。
clip が null の場合はコード駆動にフォールバック。

```
if (node.animationClip != null)
    // AnimationClip で制御（体 + 武器）
else
    // コード駆動で制御（NodePosition ベース）
```

---

## WeaponPivot の追従

WeaponPivot は ArmR（右手）の先端に追従させる必要がある。
体の姿勢が変わると右手の位置も変わるため、WeaponPivot を ArmR の子にするか、
LateUpdate で ArmR 先端のワールド座標に追従させる。

### 案: ArmR の子にする

```
ArmR_Pivot
└── ArmR（Cube）
    └── WeaponPivot ← ここに配置すれば腕と一緒に動く
        └── [武器]
```

この場合、腕が振れば武器も自動で追従する。
WeaponSwing は WeaponPivot からの相対回転を制御するだけでよい。

---

## 実装の優先順

1. 6パーツキューブモデルの組み立て（シーン上で手動 or スクリプト生成）
2. 歩行アニメーション（PlayerModelAnimator 的なスクリプト）
3. 戦闘姿勢のコード駆動（NodePosition → 体のポーズ対応）
4. WeaponPivot を ArmR の子に移動
5. AnimationClip 上書き対応（後回しでOK）

---
