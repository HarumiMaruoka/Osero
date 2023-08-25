using System;
using System.Text;
using UnityEngine;

public readonly struct CellPosition
{
    public CellPosition(int row, int column)
    {
        if (row < 0 || row > 7) { throw new ArgumentOutOfRangeException(nameof(row)); }
        if (column < 0 || column > 7) { throw new ArgumentOutOfRangeException(nameof(column)); }

        Row = row;
        Column = column;
    }

    public int Row { get; }
    public int Column { get; }

    public string Record => $"{(char)('A' + Column)}{(char)('1' + Row)}";
    public override string ToString() => $"Row={Row}, Column={Column}";
}

public static class CellPositionConverter
{
    private static string ToRecord(CellPosition[] positions)
    {
        var s = new StringBuilder();
        foreach (var position in positions) { s.Append(position.Record); }
        return s.ToString();
    }

    private static CellPosition[] Parse(string data)
    {
        if (data == null) { throw new ArgumentNullException(nameof(data)); }
        if (data.Length == 0) { return Array.Empty<CellPosition>(); }

        data = data
            .Trim()// 頭と末尾から空白を削る
            .ToUpper(); // 大文字に変換
        if (data.Length % 2 != 0) { throw new ArgumentException("不正データ", nameof(data)); }

        var result = new CellPosition[data.Length / 2];
        for (var i = 0; i < result.Length; i++)
        {
            var offset = i * 2;
            var a = data[offset]; // 列文字
            var b = data[offset + 1]; // 行文字

            if (a < 'A' || a > 'H') { throw new ArgumentOutOfRangeException(nameof(data)); }
            if (b < '1' || b > '8') { throw new ArgumentOutOfRangeException(nameof(data)); }

            var r = b - '1'; // 行番号
            var c = a - 'A'; // 列番号
            result[i] = new CellPosition(r, c);
        }
        return result;
    }
}