using Custom;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridLayoutFlexible : MonoBehaviour
{
    [SerializeField] float minSize = 300;
    [SerializeField] int minColumeCount = 2;

    [SerializeField]GridLayoutGroup _gridLayout;
    [SerializeField]RectTransform _rectTransform;

    private void Start()
    {
        Init();
    }

    void Init()
    {
        if (_gridLayout == null) { return; }

        float w = _rectTransform.rect.width;


        float size = w / minColumeCount;

        //CustomDebug.Print($"{size} // {w}");

        //사이즈가 클 경우 사이즈를 확장합니다.
        if(size > minSize) {
            int cnt = Mathf.FloorToInt(w / minSize);
            if((cnt-minColumeCount) >= 1.0f) 
            {
                minColumeCount = cnt;
                size = w / cnt;
            }
        }

        //CustomDebug.Print($"{size}");

        size -= (_gridLayout.spacing.x + ((_gridLayout.padding.right+_gridLayout.padding.left)/ minColumeCount));

        //CustomDebug.Print($"{size}");

        _gridLayout.cellSize = new Vector2(size, size);
    }

    //private void Update()
    //{
    //    CustomDebug.Print($"{_rectTransform.rect.width}");
    //}
}
