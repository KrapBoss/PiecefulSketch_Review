using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일회성 UI에 대해 초기값 세팅의 필요성이 없는 것을 Stack에 등록하여 사용합니다.
/// </summary>
public interface IActiveUI 
{
    public void Active(UIStack stack);   // 활성
    public void DeActive(); //비활성
    public void Delete();   //제거
}
