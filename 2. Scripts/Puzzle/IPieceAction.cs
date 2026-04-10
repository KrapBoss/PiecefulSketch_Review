using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//퍼즐을 들고 있는 중이거나 놨을 경우 각 퍼즐 조각의 동작을 수행합니다.
//특정 동작을 정의하여 담을 수 있습니다.
public interface IPieceAction
{
    public void StartTransfer(Piece piece);
    public void Fit(Piece piece);
}
