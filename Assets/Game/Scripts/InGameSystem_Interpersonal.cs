// 日本語対応
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 対人用オセロシステム
/// </summary>
public class InGameSystem_Interpersonal : MonoBehaviour
{
    [SerializeField]
    private ClickableGenerator _generator = default;

    /// <summary> 現在のターン </summary>
    private UserColor _currentUser = UserColor.White;
    /// <summary> ステージ </summary>
    private ClickableObj[][] _stage = null;

    private HashSet<ClickableObj> _putableObjects = new HashSet<ClickableObj>();
    private HashSet<ClickableObj> _currentTurnPutableObjects = new HashSet<ClickableObj>();

    public HashSet<ClickableObj> PutableObjects => _putableObjects;
    public HashSet<ClickableObj> CurrentTurnPutableObjects => _currentTurnPutableObjects;
    public UserColor CurrentUser => _currentUser;

    private void Awake()
    {
        _stage = _generator.Generate();
        _generator.OnComplete?.Invoke();
        _generator.OnComplete = null;
        ChangeUser();
    }

    public void ChangeUser()
    {
        // これから石を打つ人を更新
        _currentUser = _currentUser == UserColor.White ? UserColor.Black : UserColor.White;

        // これから石を打つ人の色を取得
        var myColor = _currentUser == UserColor.White ? ClickableObj.PieceStatus.White : ClickableObj.PieceStatus.Black;
        // 相手の色を取得
        var enemyColor = _currentUser == UserColor.White ? ClickableObj.PieceStatus.Black : ClickableObj.PieceStatus.White;

        // どこに打てるかを保存しておくコレクションをリセット
        _currentTurnPutableObjects.Clear();

        // 走査用変数
        int x, y;

        // 外周マスを全て走査する（これから石を打つ人がどこに打てるかを取得, 保存しておく。）
        foreach (var e in PutableObjects)
        {
            foreach (var item in e.TurnableClickables)
            {
                item.Value.Clear();
            }

            bool isFind = false;
            /* 右向きに走査 */
            x = e.Position.x + 1; y = e.Position.y; // 走査の開始位置を設定。
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => x++, myColor, enemyColor, e, Direction.Right);
            /* 左向きに走査 */
            x = e.Position.x - 1; y = e.Position.y;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => x--, myColor, enemyColor, e, Direction.Left);
            /* 上向きに走査 */
            x = e.Position.x; y = e.Position.y + 1;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => y++, myColor, enemyColor, e, Direction.Up);
            /* 下向きに走査 */
            x = e.Position.x; y = e.Position.y - 1;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => y--, myColor, enemyColor, e, Direction.Down);
            /* 右上向きに走査 */
            x = e.Position.x + 1; y = e.Position.y + 1;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => { x++; y++; }, myColor, enemyColor, e, Direction.UpperRight);
            /* 左下向きに走査 */
            x = e.Position.x - 1; y = e.Position.y - 1;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => { x--; y--; }, myColor, enemyColor, e, Direction.LowerLeft);
            /* 右下向きに走査 */
            x = e.Position.x + 1; y = e.Position.y - 1;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => { x++; y--; }, myColor, enemyColor, e, Direction.LowerRight);
            /* 左上向きに走査 */
            x = e.Position.x - 1; y = e.Position.y + 1;
            isFind |= SearchPuttablePositions(ref x, ref y, () => IsInStage(x, y), () => { x--; y++; }, myColor, enemyColor, e, Direction.UpperLeft);

            if (isFind)
            {
                _currentTurnPutableObjects.Add(e);
            }
        }
    }
    /// <summary>
    /// 指定されたマスが打てるマスかどうかを返す。
    /// </summary>
    /// <param name="x"> x座標 初期値 </param>
    /// <param name="y"> y座標 初期値 </param>
    /// <param name="condition"> 条件式 </param>
    /// <param name="increment"> 変化式 </param>
    /// <param name="ownPieceStatus"> 自分の色 </param>
    /// <param name="enemyPieceStatus"> 相手の色 </param>
    /// <returns> 置けるマスであれば true,置けないマスであれば false を返す。 </returns>
    private bool SearchPuttablePositions(ref int x, ref int y, Func<bool> condition, Action increment,
        ClickableObj.PieceStatus ownPieceStatus, ClickableObj.PieceStatus enemyPieceStatus, ClickableObj origin, Direction dir)
    {
        // 置けるマスかどうか
        bool isFind = false;
        // 相手の色を挟んでいるかどうか
        int enemyCount = 0;

        for (; condition(); increment())
        {
            var result = WhitePieceDecision(x, y, enemyCount, ownPieceStatus, enemyPieceStatus, origin, dir);
            if (result == SearchResult.Success)
            {
                isFind = true;
                break;
            }
            else if (result == SearchResult.Failed)
            {
                break;
            }
            else if (result == SearchResult.Continuation)
            {
                enemyCount++;
            }
        }

        return isFind;
    }
    private SearchResult WhitePieceDecision(int x, int y, int enemyCount,
        ClickableObj.PieceStatus myColor, ClickableObj.PieceStatus enemyColor, ClickableObj origin, Direction dir)
    {
        // 石が置かれていなければ 何もしない
        if (_stage[y][x].Status == ClickableObj.PieceStatus.None)
        {
            origin.TurnableClickables[dir].Clear();
            return SearchResult.Failed;
        }
        // すぐ隣に同じ色の石が置かれていれば 何もしない
        else if (enemyCount == 0 && _stage[y][x].Status == myColor)
        {
            return SearchResult.Failed;
        }
        // 黒を挟んで白が見つかったらそのマスを保存する
        else if (enemyCount > 0 && _stage[y][x].Status == myColor)
        {
            return SearchResult.Success;
        }
        // 相手の色であれば カウントする
        else if (_stage[y][x].Status == enemyColor)
        {
            origin.TurnableClickables[dir].Add(_stage[y][x]);
            return SearchResult.Continuation;
        }

        return SearchResult.Error;
    }
    private enum SearchResult
    {
        Success,
        Failed,
        Continuation,
        Error,
    }
    public void PutPiece(int x, int y)
    {
        AddPutable(x, y + 1); // 上
        AddPutable(x, y - 1); // 下
                              // 左右
        AddPutable(x + 1, y); // 右
        AddPutable(x - 1, y); // 左
                              // 斜め
        AddPutable(x + 1, y + 1); // 右上
        AddPutable(x + 1, y - 1); // 左上
        AddPutable(x - 1, y - 1); // 左下
        AddPutable(x - 1, y + 1); // 右下
    }

    public void RemovePutable(int x, int y)
    {
        // ステージの範囲外なら無視する。
        if (!IsInStage(x, y)) return;

        _putableObjects.Remove(_stage[y][x]);
    }
    private bool IsInStage(int x, int y)
    {
        return x >= 0 && y >= 0 && _stage.Length > y && _stage[y].Length > x;
    }
    private void AddPutable(int x, int y)
    {
        // ステージの範囲外なら無視する。
        if (x < 0 || y < 0 || _stage.Length <= y || _stage[y].Length <= x) return;
        // 何か置かれていたら無視する。
        if (_stage[y][x] == null || _stage[y][x].Status != ClickableObj.PieceStatus.None) return;

        _putableObjects.Add(_stage[y][x]);
    }
    public enum UserColor
    {
        Black, White
    }
}
public enum Direction
{
    Right, Left, Up, Down, UpperRight, LowerRight, UpperLeft, LowerLeft
}