using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// 입력 데이터
/// </summary>
public class InputData : MonoBehaviour
{
    public enum TouchState
    {
        Down, Move, Up
    }

    public Vector2 S2WTouchPosition;
    public Vector2 OriginTouchPosition;
    public TouchState touchState;
    public float Scroll;

    public bool UseOtherScroll = false;


    /// <summary> 터치된 곳에 있는 UI 이름 </summary>
    public List<string> RayUIName1 = new();
    public List<string> RayUIName2 = new();
    public void ClearUIName()
    {
        RayUIName1.Clear();
        RayUIName2.Clear();
    }

    public bool CompareDoubleTouchName(string n)
    {
        return RayUIName1.Contains(n) && RayUIName2.Contains(n);
    }

    public bool CompareDoubleTouch1Name(string n)
    {
        return RayUIName1.Contains(n);
    }
}
