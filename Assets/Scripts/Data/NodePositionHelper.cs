/// <summary>
/// NodePosition の接続ルールとフォロースルー対角計算。
/// </summary>
public static class NodePositionHelper
{
    // 8方向の円形配列（インデックスで隣接判定に使う）
    private static readonly NodePosition[] Ring =
    {
        NodePosition.Upper,
        NodePosition.UpperRight,
        NodePosition.Right,
        NodePosition.LowerRight,
        NodePosition.Lower,
        NodePosition.LowerLeft,
        NodePosition.Left,
        NodePosition.UpperLeft
    };

    /// <summary>
    /// 終了位置から開始位置へ接続可能か判定する。
    /// </summary>
    public static bool CanConnect(NodePosition endPos, NodePosition startPos)
    {
        // 手前（Back）からはどこへでも接続可能
        if (endPos == NodePosition.Back)
            return true;

        // 前（Front）からは前のみ
        if (endPos == NodePosition.Front)
            return startPos == NodePosition.Front;

        // 8方向: 自身 + 隣接1つずつ（計3方向）
        int endIndex = GetRingIndex(endPos);
        if (endIndex < 0) return false;

        int startIndex = GetRingIndex(startPos);
        if (startIndex < 0) return false; // Front/Back は8方向の隣接先にならない

        int diff = (startIndex - endIndex + 8) % 8;
        return diff == 0 || diff == 1 || diff == 7;
    }

    /// <summary>
    /// フォロースルー時の対角位置を返す。
    /// </summary>
    public static NodePosition GetOpposite(NodePosition pos)
    {
        if (pos == NodePosition.Front) return NodePosition.Back;
        if (pos == NodePosition.Back) return NodePosition.Front;

        int index = GetRingIndex(pos);
        if (index < 0) return pos;

        return Ring[(index + 4) % 8];
    }

    private static int GetRingIndex(NodePosition pos)
    {
        for (int i = 0; i < Ring.Length; i++)
        {
            if (Ring[i] == pos) return i;
        }
        return -1;
    }
}
