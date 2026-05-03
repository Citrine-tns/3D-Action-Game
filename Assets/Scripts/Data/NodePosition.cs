/// <summary>
/// コンボノードの開始/終了位置（10方向）
///
///     左上    上    右上
///       ＼   ｜   ／
///   左 ── ・ ── 右
///       ／   ｜   ＼
///     左下    下    右下
///
///   前（突き方向）  手前（引き戻し / ニュートラル）
/// </summary>
public enum NodePosition
{
    Upper,       // 上
    UpperRight,  // 右上
    Right,       // 右
    LowerRight,  // 右下
    Lower,       // 下
    LowerLeft,   // 左下
    Left,        // 左
    UpperLeft,   // 左上
    Front,       // 前（突き）
    Back         // 手前（引き戻し）
}
