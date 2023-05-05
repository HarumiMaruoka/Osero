// 日本語対応
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class ClickableObj : MonoBehaviour
{
    [SerializeField]
    private Color _mouseOverColor = Color.yellow;
    [SerializeField]
    private GameObject _piecePrefab = default;
    [SerializeField]
    private Vector3 _pieceOffset = default;

    private InGameSystem_Interpersonal _system = null;
    private Material _material = null;
    private PieceStatus _status = PieceStatus.None;
    private GameObject _piece = null;
    private Vector2Int _position;
    private Dictionary<Direction, HashSet<ClickableObj>> _turnableClickables = new Dictionary<Direction, HashSet<ClickableObj>>()
    {{Direction.Right,new HashSet<ClickableObj>() },
    {Direction.Left,new HashSet<ClickableObj>() },
    {Direction.Up,new HashSet<ClickableObj>() },
    {Direction.Down,new HashSet<ClickableObj>() },
    {Direction.UpperRight,new HashSet<ClickableObj>() },
    {Direction.UpperLeft,new HashSet<ClickableObj>() },
    {Direction.LowerRight,new HashSet<ClickableObj>() },
    {Direction.LowerLeft,new HashSet<ClickableObj>() },};

    public PieceStatus Status => _status;
    public Vector2Int Position => _position;
    public Dictionary<Direction, HashSet<ClickableObj>> TurnableClickables => _turnableClickables;

    public void SetPos(Vector2Int position)
    {
        _position = position;
    }
    public void SetSystem(InGameSystem_Interpersonal system)
    {
        _system = system;
    }
    private void Awake()
    {
        _material = GetComponent<MeshRenderer>().material;
    }

    private void OnMouseDown()
    {
        if (_status != PieceStatus.None) return;
        if (!_system.CurrentTurnPutableObjects.Contains(this)) return;

        if (_system.CurrentUser == InGameSystem_Interpersonal.UserColor.White)
        {
            OnWhite();
        }
        else
        {
            OnBlack();
        }
        _system.ChangeUser();
    }

    public void OnWhite()
    {
        _piece = Instantiate(_piecePrefab, transform.position + _pieceOffset, Quaternion.Euler(0f, 0f, 0f), transform);
        _system.PutPiece(_position.x, _position.y);
        _system.RemovePutable(_position.x, _position.y);
        _status = PieceStatus.White;

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
        _status = PieceStatus.Black;

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
        // 黒なら白, 白なら黒 に反転する
        _status = _status == PieceStatus.Black ? PieceStatus.White : PieceStatus.Black;
        if (_dotween != null) _dotween.Kill();
        if (_status == PieceStatus.White)
        {
            _dotween = _piece.transform.DORotate(new Vector3(0f, 0f, 0f), 0.6f).OnComplete(() => _dotween = null);
        }
        else
        {
            _dotween = _piece.transform.DORotate(new Vector3(180f, 0f, 0f), 0.6f).OnComplete(() => _dotween = null);
        }
    }
    private void OnMouseEnter()
    {
        if (_status != PieceStatus.None) return;
        if (!_system.CurrentTurnPutableObjects.Contains(this)) return;

        _material.color = _mouseOverColor;
    }
    private void OnMouseExit()
    {
        _material.color = Color.green;
    }
    public enum PieceStatus
    {
        None,
        Black,
        White
    }
}

