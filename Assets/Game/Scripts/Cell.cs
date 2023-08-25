// 日本語対応
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField]
    private GameObject _piecePrefab = default;
    [SerializeField]
    private Vector3 _pieceOffset = default;

    private InterpersonalGameSystem _system = null;
    private Material _material = null;
    private CellStatus _status = CellStatus.None;
    private GameObject _piece = null;
    private Vector2Int _position;
    private Dictionary<Direction, HashSet<Cell>> _turnableClickables = new Dictionary<Direction, HashSet<Cell>>()
    {{Direction.Right,new HashSet<Cell>() },
    {Direction.Left,new HashSet<Cell>() },
    {Direction.Up,new HashSet<Cell>() },
    {Direction.Down,new HashSet<Cell>() },
    {Direction.UpperRight,new HashSet<Cell>() },
    {Direction.UpperLeft,new HashSet<Cell>() },
    {Direction.LowerRight,new HashSet<Cell>() },
    {Direction.LowerLeft,new HashSet<Cell>() },};

    public CellStatus Status => _status;
    public Vector2Int Position => _position;
    public Dictionary<Direction, HashSet<Cell>> TurnableCells => _turnableClickables;

    public void SetPosition(Vector2Int position)
    {
        _position = position;
    }
    public void SetSystem(InterpersonalGameSystem system)
    {
        _system = system;
    }
    private void Awake()
    {
        _material = GetComponent<MeshRenderer>().material;
    }

    private void OnMouseDown()
    {
        // セルに既に石が配置されていれば無効。
        if (_status != CellStatus.None) return;
        // 現在ターンの人が打てる場所リストに含まれていなければ無効。
        if (!_system.CurrentTurnPutableCells.Contains(this)) return;

        // 現在ターンの人の色が白であれば白の石を配置する。そうでなければ黒を配置する。
        if (_system.CurrentUser.Value == InterpersonalGameSystem.UserColor.White)
        {
            OnWhite();
        }
        else
        {
            OnBlack();
        }
        // ターンを更新する。
        _system.ChangeUser();
    }

    public void OnWhite()
    {
        _piece = Instantiate(_piecePrefab, transform.position + _pieceOffset, Quaternion.Euler(0f, 0f, 0f), transform);
        _system.PutPiece(_position.x, _position.y);
        _system.RemovePutable(_position.x, _position.y);
        _status = CellStatus.White;

        foreach (var e in _turnableClickables)
        {
            foreach (var item in e.Value)
            {
                item.Turn();
            }
        }
    }
    public void OnBlack()
    {
        _piece = Instantiate(_piecePrefab, transform.position + _pieceOffset, Quaternion.Euler(180f, 0f, 0f), transform);
        _system.PutPiece(_position.x, _position.y);
        _system.RemovePutable(_position.x, _position.y);
        _status = CellStatus.Black;

        foreach (var e in _turnableClickables)
        {
            foreach (var item in e.Value)
            {
                item.Turn();
            }
        }
    }
    TweenerCore<Quaternion, Vector3, QuaternionOptions> _dotween = null;
    private void Turn()
    {
        // 黒なら白, 白なら黒 に反転する。
        _status = _status == CellStatus.Black ? CellStatus.White : CellStatus.Black;

        // 反転アニメーション処理。
        if (_dotween != null) _dotween.Kill(); // 再生中ならキルする。
        if (_status == CellStatus.White)
        {
            _dotween = _piece.transform.DORotate(new Vector3(0f, 0f, 0f), 0.6f).OnComplete(() => _dotween = null);
        }
        else
        {
            _dotween = _piece.transform.DORotate(new Vector3(180f, 0f, 0f), 0.6f).OnComplete(() => _dotween = null);
        }
    }
    public void ChageColor(Color color)
    {
        _material.color = color;
    }
    private void OnMouseEnter()
    {
        if (_status != CellStatus.None) return;
        if (!_system.CurrentTurnPutableCells.Contains(this)) return;

        ChageColor(Color.yellow);
    }
    private void OnMouseExit()
    {
        if (_system.CurrentTurnPutableCells.Contains(this))
        {
            ChageColor(Color.blue);
            return;
        }
        else
        {
            ChageColor(Color.green);
            return;
        }
    }
    public enum CellStatus
    {
        None,
        Black,
        White
    }
}