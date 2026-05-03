# プロジェクト全体レポート

最終更新: 2026-05-03

---

## ファイル構成

```
Assets/Scripts/ (26ファイル)
├── Camera/     CameraController.cs, LockOnController.cs
├── Combat/     ComboRunner.cs, DamageCalculator.cs, Health.cs, Hitbox.cs,
│               Hurtbox.cs, PlayerCombatController.cs, WeaponHolder.cs, WeaponSwing.cs
├── Data/       AttributeResistance.cs, ComboNodeData.cs, ComboPreset.cs,
│               NodePosition.cs, NodePositionHelper.cs, PhysicalAttribute.cs,
│               WeaponBasePose.cs, WeaponData.cs, WeaponType.cs
├── Player/     PlayerInputActions.cs (自動生成), PlayerInputReader.cs,
│               PlayerMotor.cs, PlayerWalkAnimation.cs
└── UI/         ComboDebugUI.cs, EnemyHpBar.cs, HpBar.cs

Assets/GameData/
├── Animations/  DiagonalSlash_GSword.anim, SoaringSwallowSlash_GSowrd.anim, Player.controller
├── ComboNodes/  DiagonalSlash_GSword.asset, SoaringSwallowSlash_GSowrd.asset
├── ComboPresets/ GreatSwordCombo.asset
└── Weapons/     Club, Dagger, GreatSword, Gun, Hammer, Spear (.asset ×6)

Docs/systems/ (8ファイル)
  camera_movement.md, combat_overview.md, combo_system.md,
  player_model_and_animation.md, player_movement.md,
  target_lockon.md, weapon_swing.md, weapons.md
```

---

## 実装済みシステム

| システム | 状態 | 備考 |
|---------|------|------|
| プレイヤー移動 | 完了 | カメラ相対・プレイヤー相対・コンボ中ロック |
| カメラ制御（通常+ロックオン） | 完了 | SphereCast衝突回避、FOVベース距離調整 |
| ターゲットロックオン | 完了 | 4状態(Normal/Strafe/Locked/RetargetGrace) |
| コンボ状態マシン | 完了 | Idle→Active→Recovery。入力バッファリング |
| コンボプリセット+接続ルール | 完了 | 10方向、隣接接続、フォロースルー対角反転 |
| ダメージ計算 | 完了 | 基礎×ノード倍率×属性相性×最終段補正×背面補正 |
| 属性相性（斬壊突） | 完了 | 3属性×耐性倍率 |
| Hitbox/Hurtbox | 完了 | トリガー判定、同スイング重複防止 |
| HP管理 | 完了 | イベント駆動（OnHpChanged, OnDied） |
| 武器データ定義 | 完了 | 6武器アセット作成済み |
| 武器生成・装備 | 完了 | WeaponHolder で実行時生成 |
| 武器スイング（コード駆動） | 完了 | Bezier曲線補間、10方向対応 |
| 武器スイング（AnimationClip） | 完了 | SampleAnimation + crossfade遷移 |
| 10パーツキャラモデル | 完了 | ヒエラルキー手動配置（肘・膝関節あり） |
| 歩行アニメーション | 完了 | Sin波+肘膝屈伸、武器別振り幅スケール |
| 武器基本姿勢 | 完了 | WeaponBasePose で全身+歩行パラメータ |
| Idle→構え遷移 | 完了 | RotateTowards/crossfade（速度ベース） |
| HPバーUI（プレイヤー） | 完了 | Slider連動 |
| HPバーUI（敵頭上） | 完了 | World Space Canvas + ビルボード |
| コンボデバッグUI | 完了 | 状態表示 |

---

## バグ・問題点

### 高優先度

| # | ファイル | 行 | 問題 |
|---|---------|-----|------|
| 1 | ComboDebugUI.cs | 25 | `comboRunner.currentPreset.slots.Count` — currentPreset が null の時に NullReferenceException |
| 2 | WeaponSwing.cs | 97 | Recovery 中の `currentClip.SampleAnimation` — currentClip が null になる経路がある（Idle→Recovery の直接遷移は通常ないが防御不足） |
| 3 | PlayerMotor.cs | 36 | `lockOn.IsStrafing` — lockOn 未設定で NullReferenceException |
| 4 | WeaponSwing.cs | 構え遷移全般 | 遷移中に ComboRunner が Recovery に移行する問題（ExtendActiveTimer で対処済みだが、遷移速度が極端に遅い場合にまだ発生しうる） |

### 中優先度

| # | ファイル | 行 | 問題 |
|---|---------|-----|------|
| 5 | PlayerCombatController.cs | 29 | `hitbox.gameObject.SetActive(false)` — hitbox null チェックなし（42行目にはある） |
| 6 | Health.cs | 全般 | イベント購読者が OnDestroy で解除されない（メモリリーク可能性） |
| 7 | EnemyHpBar.cs | 59 | `FindAnyObjectByType<Camera>()` が OnEnable のたびに呼ばれる（パフォーマンス） |
| 8 | WeaponHolder.cs | 48 | `CreatePrimitive(Cylinder)` で生成される Collider を即 Destroy（無駄なアロケーション） |

### アセット命名

| ファイル | 問題 |
|---------|------|
| `SoaringSwallowSlash_GSowrd.asset` | タイポ: GSowrd → GSword |
| `SoaringSwallowSlash_GSowrd.anim` | 同上 |

---

## 未実装（仕様書に記載あり / 設計上想定される）

| # | 機能 | 根拠 |
|---|------|------|
| 1 | **敵AI / 敵の行動** | Hurtbox/Health はあるが敵スクリプトなし |
| 2 | **死亡処理** | Health.OnDied イベントはあるが購読するコードなし |
| 3 | **武器切り替え** | combat_overview.md「将来的に複数武器の持ち替えを想定」 |
| 4 | **ノード解放システム** | ComboNodeData.requiredLevel フィールドはあるがチェックロジックなし |
| 5 | **コンボ設定UI（メニュー画面）** | combo_system.md に設計あり、未実装 |
| 6 | **フォロースルー回しアニメーション** | 仕様確定済み、コード未実装 |
| 7 | **槌の衝撃波** | weapons.md「叩きつけノードで衝撃波を発生」 |
| 8 | **銃の射撃（遠距離攻撃）** | 現在の Hitbox は近接専用 |
| 9 | **スタン** | WeaponData.canStun フィールドあり、ロジックなし |
| 10 | **プレイヤー非表示処理** | camera_movement.md「カメラ距離が一定以下でモデル非表示」 |
| 11 | **各武器のコンボノード群** | 大剣2ノードのみ。他5武器のノード未作成 |
| 12 | **左腕の戦闘姿勢連動** | 両手持ち武器の左腕制御は basePose で定義可能だが、コンボ中の左腕モーション未実装 |

---

## 最適化可能な箇所

| # | 箇所 | 内容 |
|---|------|------|
| 1 | WeaponSwing.allTransforms | `GetComponentsInChildren<Transform>()` を Initialize で1回だけ呼ぶのは良いが、ヒエラルキー変更時にキャッシュが古くなる |
| 2 | WeaponHolder.Equip | `CreatePrimitive` で Collider が自動生成されて即 Destroy — `new GameObject` + `MeshFilter` + `MeshRenderer` で直接構築すれば無駄を省ける |
| 3 | ComboDebugUI | 毎フレーム文字列生成（`$"..."` + 連結）— StringBuilder やキャッシュで GC 削減可能 |
| 4 | NodePositionHelper.GetRingIndex | 線形探索 — enum 値を直接インデックスとして使える設計にすれば O(1) |

---
