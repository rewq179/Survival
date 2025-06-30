using UnityEngine;
using System.Collections.Generic;

public abstract class BaseReader : ScriptableObject
{
    public string sheetAddress = "15RA8mCckMbGIXxx5Ukfmtih0TxDTEGkXiAVyrHf8Blw";
    public abstract string sheetName { get; }
    [Header("읽기 시작할 행 번호")][SerializeField] public int startRow = 2;
    [Header("읽을 마지막 행 번호")][SerializeField] public int endRow = -1;
}
