// 日本語対応
using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// 対人用オセロシステム
/// </summary>
public class InterpersonalGameSystem : MonoBehaviour
{
    [SerializeField]
    private StageGenerator _generator = default;

    /// <summary> 現在のターン </summary>
    private ReactiveProperty<UserColor> _currentUser = new ReactiveProperty<UserColor>(UserColor.White);
    /// <summary> ステージ </summary>
    private Cell[][] _stage = null;

    private HashSet<Cell> _putableCells = new HashSet<Cell>();
    private HashSet<Cell> _currentTurnPutableCells = new HashSet<Cell>();

    public HashSet<Cell> PutableCells => _putableCells; // 全ての配置可能なセルリスト
    public HashSet<Cell> CurrentTurnPutableCells => _currentTurnPutableCells; // 現在ターンの人が配置可能なセルリスト
    public IReadOnlyReactiveProperty<UserColor> CurrentUser => _currentUser;

    private bool _isSetuped = false;
    private int _skipCount = 0;
    private bool _isEnd = false;

    private void Awake()
    {
        _stage = _generator.Generate();
        _generator.OnGenerated?.Invoke();
        _generator.OnGenerated = null;
        _isSetuped = true;
        ChangeUser();
    }

    public void ChangeUser()
    {
        if (_isEnd) return;
        // これから石を打つ人を更新
        _currentUser.Value = _currentUser.Value == UserColor.White ? UserColor.Black : UserColor.White;
        // これから石を打つ人の色を取得
        var myColor = _currentUser.Value == UserColor.White ? Cell.CellStatus.White : Cell.CellStatus.Black;
        // 相手の色を取得
        var enemyColor = _currentUser.Value == UserColor.White ? Cell.CellStatus.Black : Cell.CellStatus.White;

        // 現在ターンの人用に、新しく配置可能なセルを計算するのでリストをクリア。
        _currentTurnPutableCells.Clear();

        // 新しく配置可能なセルを計算する走査用変数。
        int x, y;

        // 全ての配置可能セルを全て走査し、現在ターンの人が配置可能なセルを抽出する。
        foreach (var e in PutableCells)
        {
            foreach (var item in e.TurnableCells)
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
                e.ChageColor(Color.blue);
                _currentTurnPutableCells.Add(e);
            }
            else
            {
                e.ChageColor(Color.green);
            }
        }
        // ここに勝敗の処理を記述する。（打つ場所がなくなったときがオセロの終わり。）
        if (PutableCells.Count == 0 || _skipCount == 2)
        {
            _isEnd = true;
            int blackCount = 0;
            int whiteCount = 0;

            for (int yCounter = 0; yCounter < _stage.Length; yCounter++)
            {
                for (int xCounter = 0; xCounter < _stage[yCounter].Length; xCounter++)
                {
                    if (_stage[yCounter][xCounter] == null) continue;

                    if (_stage[yCounter][xCounter].Status == Cell.CellStatus.Black)
                    {
                        blackCount++;
                    }
                    else if (_stage[yCounter][xCounter].Status == Cell.CellStatus.White)
                    {
                        whiteCount++;
                    }
                }
            }

            if (blackCount > whiteCount)
            {
                Debug.Log("黒の勝ち！");
            }
            else if (whiteCount > blackCount)
            {
                Debug.Log("白の勝ち！");
            }
            else
            {
                Debug.Log("引き分け！");
            }
            return;
        }

        if (_isEnd) return;
        // スキップ処理（現在ターンの人が打つ手がないとき。）
        if (_currentTurnPutableCells.Count == 0 && _isSetuped)
        {
            _skipCount++;
            ChangeUser();
            return;
        }
        _skipCount = 0;
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
        Cell.CellStatus ownPieceStatus, Cell.CellStatus enemyPieceStatus, Cell origin, Direction dir)
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
            else if (result == SearchResult.Failed ||
                     result == SearchResult.Error)
            {
                break;
            }
            else if (result == SearchResult.Continuation)
            {
                enemyCount++;
            }
        }
        if (!isFind)
        {
            origin.TurnableCells[dir].Clear();
        }
        return isFind;
    }
    private SearchResult WhitePieceDecision(int x, int y, int enemyCount,
        Cell.CellStatus myColor, Cell.CellStatus enemyColor, Cell origin, Direction dir)
    {
        // 石が置かれていなければ 何もしない
        if (_stage[y][x].Status == Cell.CellStatus.None)
        {
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
            origin.TurnableCells[dir].Add(_stage[y][x]);
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

        AddPutable(x + 1, y); // 右
        AddPutable(x - 1, y); // 左

        AddPutable(x + 1, y + 1); // 右上
        AddPutable(x + 1, y - 1); // 右下
        AddPutable(x - 1, y - 1); // 左下
        AddPutable(x - 1, y + 1); // 左上
    }

    public void RemovePutable(int x, int y)
    {
        // ステージの範囲外なら無視する。
        if (!IsInStage(x, y)) return;

        _putableCells.Remove(_stage[y][x]);
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
        if (_stage[y][x] == null || _stage[y][x].Status != Cell.CellStatus.None) return;

        _putableCells.Add(_stage[y][x]); // 配置可能リストに追加
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