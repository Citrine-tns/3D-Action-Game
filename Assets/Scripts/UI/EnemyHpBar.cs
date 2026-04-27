using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敵の頭上に表示するHPバー。World Space Canvas を使う。
///
/// 【セットアップ】
///
/// ■ 敵オブジェクトの下に Canvas を作る
/// 1. 敵オブジェクトを右クリック → UI → Canvas
///    - Render Mode を "World Space" に変更
///    - RectTransform の Width: 1, Height: 0.15
///    - Scale を X: 1, Y: 1, Z: 1 に
///    - Pos Y を敵の頭上あたりに設定（例: 2.2）
///    - Canvas の "Sorting Order" を 1 に（他UIの上に出す）
///    ※ EventSystem は既にあるなら不要（重複するとエラーになるので削除）
///
/// 2. その Canvas を右クリック → UI → Slider
///    - RectTransform: Anchor を stretch-stretch に, Left/Right/Top/Bottom 全部 0
///    - Interactable: OFF
///    - Handle Slide Area を削除
///    - Background の Color: 暗い色
///    - Fill の Color: 赤
///
/// ■ スクリプト
/// 3. 敵オブジェクト（Health と同じ階層）にこのスクリプトをアタッチ
/// 4. slider に作った Slider をセット
/// 5. mainCamera は空でOK（自動取得）
/// </summary>
public class EnemyHpBar : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;

    [Header("Settings")]
    public bool hideWhenFull = true;

    private Health health;
    private Canvas canvas;
    private Transform mainCamera;

    private void Awake()
    {
        health = GetComponent<Health>();
        canvas = slider.GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHpChanged += OnHpChanged;
        }

        if (mainCamera == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam != null) mainCamera = cam.transform;
        }
    }

    private void Start()
    {
        // Start は全 Awake の後に呼ばれるので、Health の初期化済み値を読める
        if (health != null)
        {
            OnHpChanged(health.CurrentHp, health.maxHp);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHpChanged -= OnHpChanged;
        }
    }

    private void LateUpdate()
    {
        if (canvas == null || mainCamera == null) return;

        // ビルボード：Canvas の前面（Z-）をカメラに向ける
        canvas.transform.forward = mainCamera.forward;
    }

    private void OnHpChanged(int current, int max)
    {
        if (max <= 0) return;

        slider.value = (float)current / max;

        if (hideWhenFull && canvas != null)
        {
            canvas.gameObject.SetActive(current < max);
        }
    }
}
