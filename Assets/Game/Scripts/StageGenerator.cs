// 日本語対応
using System;
using UnityEngine;

public class StageGenerator : MonoBehaviour
{
    [SerializeField]
    private TextAsset _stageData = default;
    [SerializeField]
    private float _objectSpacing = 1f;
    [SerializeField]
    private InterpersonalGameSystem _system = default;
    [SerializeField]
    private Cell _clickablePrefab = default;
    [SerializeField]
    private Vector3 _generatePositionOffset = default;

    public Action OnGenerated = default;

    [SerializeField]
    private string _restoreData;

    public Cell[][] Generate()
    {
        var line = _stageData.text.Split('\n');
        for (int i = 0; i < line.Length; i++)
        {
            line[i] = line[i].TrimEnd('\r', '\n');
        }
        var result = new Cell[line.Length][];

        for (int y = 0; y < result.Length; y++)
        {
            result[y] = new Cell[line[y].Length];

            for (int x = 0; x < result[y].Length; x++)
            {
                // 0, 1, 2 以外は無効
                if (line[y][x] < '0' || line[y][x] > '2') continue;

                var clickable = Instantiate(_clickablePrefab,
                    new Vector3(x * _objectSpacing + _generatePositionOffset.x,
                                _generatePositionOffset.y,
                                y * _objectSpacing + _generatePositionOffset.z),
                    Quaternion.identity, transform);

                clickable.SetPosition(new Vector2Int(x, y));
                clickable.SetSystem(_system);
                result[y][x] = clickable;

                if (line[y][x] == '1')
                {
                    OnGenerated += result[y][x].OnBlack;
                }
                else if (line[y][x] == '2')
                {
                    OnGenerated += result[y][x].OnWhite;
                }
            }
        }
        return result;
    }
}
