using UnityEngine;
using System.Collections.Generic;

public class BaseReader : ScriptableObject
{
    [Header("시트의 주소")][SerializeField] public string sheetAddress = "";
    [Header("스프레드 시트의 시트 이름")][SerializeField] public string sheetName = "";
    [Header("읽기 시작할 행 번호")][SerializeField] public int startRow = 2;
    [Header("읽을 마지막 행 번호")][SerializeField] public int endRow = -1;
}
