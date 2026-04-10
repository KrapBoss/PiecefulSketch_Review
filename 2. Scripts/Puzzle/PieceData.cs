using System.Collections;
using Custom;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 각 조각의 정보를 담는 데이터
/// 에디터에서 할당
/// </summary>
[System.Serializable]
public class PieceData
{
    public string PieceName;
    public Sprite Sprite;
    public Vector2 FitPosition;
    public int Sorting;
    public bool Activation= true;     // 활성화 된 조각은 아닌지 // 초기값 지정해서 움직이지 않도록 함
    public float ScaleX;        // 원본 이미지의 스케일 크기를 저장하고 사용하기 위해
    public SpriteMaskInteraction mask;
}

/// <summary>
/// 퍼즐 조각은 반드시 조각을 받아서 사용
/// </summary>
public class Piece : MonoBehaviour
{
    public PieceData data;                      //자신만의 데이터
    public SpriteRenderer[] spriteRenderer;     //자신이 가진 모든 스프라이트 렌더러.
    protected PuzzleContainer _container;       //퍼즐 컨테이너를 통해 정답을 판단
    protected IPieceAction _pieceAction;        //각 조각들의 특별한 액션을 위해
    protected PieceTransfer _pieceTransfer;     //터치를 통해 조각을 이동시킵니다.

    Action _fit, _notFit;

    //데이터를 초기화해줍니다.
    public virtual void Initialize(PuzzleContainer container)
    {
        if (container == null) throw new Exception($"퍼즐 조각의 컨테이너가 할당되지 않았습니다 => {transform.name}");

        _container = container;

        //조각의 액션을 컴포넌트를 찾습니다.
        _pieceAction = GetComponent<IPieceAction>();

        //자신만의 데이터를 생성합니다.
        SetData();


        //조각 이동을 위한 컴포넌트를 찾습니다.
        _pieceTransfer = GetComponent<PieceTransfer>();
        if (_pieceTransfer == null)
        {
            _pieceTransfer = transform.AddComponent<PieceTransfer>();
        }
        _pieceTransfer.Initialize(transform/*, new Vector2(0, -spriteRenderer[0].size.y / 2)*/);

        //조각이 최상단에 위치하도록 합니다.
        foreach (var sprite in spriteRenderer) sprite.sortingOrder = 999;

        //조각을 숨깁니다.
        HidePiece();
    }

    //자신만의 데이터를 세팅합니다.
    void SetData()
    {
        spriteRenderer = GetComponentsInChildren<SpriteRenderer>();
        data.Sorting = spriteRenderer[0].sortingOrder;
        data.Sprite = spriteRenderer[0].sprite;
        data.PieceName = gameObject.name;
        data.FitPosition = transform.localPosition;
        data.Activation = false;
        data.ScaleX = transform.localScale.x;


        data.mask = spriteRenderer[0].maskInteraction;
        foreach(var item in spriteRenderer)
        {
            item.maskInteraction = SpriteMaskInteraction.None;
        }
    }

    //조각 이동을 알립니다.0...0
    public virtual void StartTransition(Action notFit, Action fit)
    {
        if (_pieceAction != null) _pieceAction.StartTransfer(this);

        gameObject.SetActive(true);

        _pieceTransfer.StartTransfer();

        _fit = fit;
        _notFit = notFit;
    }

    //조각을 맞춰봅니다.
    public virtual bool Fit(Vector2 fitPos)
    {
        //맞추기 전의 조각은 숨김
        HidePiece();

        //같은 이름을 가진 모든 조각을 찾습니다.
        Piece[] piecesWithName = PuzzleDictionary.Instance.GetPieceWithName(data.PieceName);

        if (piecesWithName == null) { throw new Exception($"찾는 조각 이름 \"{data.PieceName}\"이 존재하지 않는 조각입니다."); }

        foreach (Piece piece in piecesWithName)
        {
            //조각의 위치가 맞는지 판단합니다.
            //그리고 비활성화 된 경우에만 조각을 맞춥니다.
            if (!piece.data.Activation)
            {
                if (!piece.IsFit(fitPos)) continue;

                //조각을 맞춘 경우 활성화시킵니다.
                _container.FitThePiece();
                piece.ActivePiece();
                if (_fit != null) _fit();

                return true;
            }
        }

        //조각이 하나도 맞지 않은 경우
        if (_notFit != null) _notFit();
        return false;
    }

    //퍼즐 판에 있는 조각과 위치가 맞는지 확인을 합니다.
    public bool IsFit(Vector2 fitPos)
    {
        if ((fitPos - data.FitPosition).magnitude <= PuzzleConfig.FitRange)
        {
            //Debug.Log($"{data.PieceName} : {data.FitPosition} : {(fitPos - data.FitPosition).magnitude}");
            return true;
        }
        else
        {
            if (ItemData.Instance.GetItemCount(ItemType.HINT) > 0)
            {
                CustomDebug.PrintW("HINT 아이템을 사용하였습니다");
                Notice.Message("used hint");
                ItemData.Instance.TrySubtractItemCount(ItemType.HINT, 1);
                return true;
            }
            return false;
        }
    }

    //조각이 제자리에 들어갔음을 활성화합니다.
    public void ActivePiece(bool bound = true)
    {
        PieceFit();
        Vibrator.Vibrate(50);
        StartCoroutine(BounceEffect());
    }

    //조각이 제자리에 들어갔음을 활성화합니다.
    public void PieceFit()
    {
        gameObject.SetActive(true);
        transform.localPosition = data.FitPosition;
        data.Activation = true;

        foreach (var sprite in spriteRenderer)
        {
            if(sprite == null) continue;
            sprite.sortingOrder = data.Sorting;
            sprite.maskInteraction = data.mask;
        }

        if (_pieceAction != null) _pieceAction.Fit(this);
    }

    IEnumerator BounceEffect()
    {

        // 2. 애니메이션
        float duration = 0.3f; // 전체 바운스 시간
        float magnitude = 0.1f; // 바운스 강도 (20% 크게)
        Vector3 originalScale = transform.localScale;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            // Sin을 이용한 간단한 바운스 곡선
            float amplitude = 1.0f + magnitude * Mathf.Sin(progress * Mathf.PI);
            transform.localScale = originalScale * amplitude;
            yield return null;
        }

        // 3. 원래 크기로 리셋하고 최종 속성 적용
        transform.localScale = originalScale;
    }

    //조각을 숨깁니다.
    void HidePiece()
    {
        //조각이 활성화 된 상태가 아니라면 화면밖으로 이동시킵니다.
        transform.localPosition += new Vector3(100, 100, 0);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 퍼즐 조각을 분할할 개수. (기본값: 1, 1일 경우 분할하지 않음)
    /// </summary>
    [SerializeField]
    public int pieceCount = 1;

    /// <summary>
    /// 퍼즐 조각을 분할할 방향.
    /// </summary>
    [SerializeField]
    public SliceDirection sliceDirection;

    /// <summary>
    /// 분할 방향을 정의합니다.
    /// </summary>
    public enum SliceDirection
    {
        Horizontal, // 가로 분할
        Vertical    // 세로 분할
    }

    private static int separatedPieceCounter = 1;

    /// <summary>
    /// 분리된 조각의 넘버링을 위한 정적 카운터를 리셋합니다.
    /// </summary>
    public static void ResetSeparatedCounter()
    {
        separatedPieceCounter = 1;
    }

    /// <summary>
    /// 원본 GameObject를 받아서 pieceCount에 따라 여러 조각으로 분할하고, 분할된 GameObject 리스트를 반환합니다.
    /// </summary>
    public static System.Collections.Generic.List<GameObject> Slice(GameObject originalPiece)
    {
        Piece originalPieceComponent = originalPiece.GetComponent<Piece>();
        if (originalPieceComponent == null) return null;

        if (originalPieceComponent.spriteRenderer == null || originalPieceComponent.spriteRenderer.Length == 0)
        {
            originalPieceComponent.SetData();
        }

        if (originalPieceComponent.spriteRenderer == null || originalPieceComponent.spriteRenderer.Length == 0 || originalPieceComponent.spriteRenderer[0] == null)
        {
            Debug.LogError($"Piece '{originalPiece.name}' is missing SpriteRenderer references, cannot slice.");
            return null;
        }

        int pieceCount = originalPieceComponent.pieceCount;
        SliceDirection sliceDirection = originalPieceComponent.sliceDirection;
        Sprite originalSprite = originalPieceComponent.spriteRenderer[0].sprite;

        if (originalSprite == null || originalSprite.texture == null)
        {
            Debug.LogError($"Sprite or Texture is missing on piece '{originalPiece.name}', cannot slice.");
            return null;
        }

        try
        {
            originalSprite.texture.GetPixel(0, 0);
        }
        catch (UnityException)
        {
            Debug.LogError("Texture is not readable. Please enable 'Read/Write Enabled' in the texture import settings.");
            return null;
        }

        Rect trimmedBounds = GetTrimmedTextureBounds(originalSprite.texture);
        System.Collections.Generic.List<GameObject> slicedPieces = new System.Collections.Generic.List<GameObject>();

        float sliceWidth = trimmedBounds.width / (sliceDirection == SliceDirection.Horizontal ? pieceCount : 1);
        float sliceHeight = trimmedBounds.height / (sliceDirection == SliceDirection.Vertical ? pieceCount : 1);

        Vector2 pivotToTextureBottomLeft = new Vector2(0 - originalSprite.pivot.x, 0 - originalSprite.pivot.y);
        Vector2 bottomLeftToTrimmedBottomLeft = new Vector2(trimmedBounds.x, trimmedBounds.y);
        Vector3 worldAnchor = originalPiece.transform.position + originalPiece.transform.TransformVector((pivotToTextureBottomLeft + bottomLeftToTrimmedBottomLeft) / originalSprite.pixelsPerUnit);

        // 그림자 처리 Step 1: 원본 그림자 분리 및 정보 저장
        Transform originalShadow = null;
        foreach (Transform child in originalPiece.transform)
        {
            if (child.name.ToLower().Contains("shadow"))
            {
                originalShadow = child;
                originalShadow.SetParent(null, true);
                break;
            }
        }

        for (int i = 0; i < pieceCount; i++)
        {
            GameObject slicedPieceObject = Instantiate(originalPiece);
            slicedPieceObject.transform.SetParent(originalPiece.transform.parent, true);
            slicedPieceObject.transform.localScale = originalPiece.transform.localScale;
            slicedPieceObject.transform.rotation = originalPiece.transform.rotation;

            slicedPieceObject.name = $"Seperated_{separatedPieceCounter++}";

            Vector3 newScale = slicedPieceObject.transform.localScale;
            if (sliceDirection == SliceDirection.Vertical)
            {
                newScale.y *= 1.01f;
            }
            else
            {
                newScale.x *= 1.01f;
            }
            slicedPieceObject.transform.localScale = newScale;

            // 그림자 처리 Step 2: 복제된 그림자 무조건 삭제
            foreach (Transform child in slicedPieceObject.transform)
            {
                if (child.name.ToLower().Contains("shadow"))
                {
                    Destroy(child.gameObject);
                    break;
                }
            }

            int x = (int)(trimmedBounds.x + (sliceDirection == SliceDirection.Horizontal ? i * sliceWidth : 0));
            int y = (int)(trimmedBounds.y + (sliceDirection == SliceDirection.Vertical ? i * sliceHeight : 0));
            int w = (int)sliceWidth;
            int h = (int)sliceHeight;

            if (w <= 0 || h <= 0) continue;

            Rect sliceRect = new Rect(x, y, w, h);
            Texture2D slicedTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            slicedTexture.SetPixels(originalSprite.texture.GetPixels(x, y, w, h));
            slicedTexture.Apply();

            Sprite newSprite = Sprite.Create(slicedTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), originalSprite.pixelsPerUnit);

            newSprite.name = $"Seperated_{separatedPieceCounter++}";
            SpriteRenderer renderer = slicedPieceObject.GetComponentInChildren<SpriteRenderer>();
            if(renderer != null) renderer.sprite = newSprite;

            Vector2 sliceCenterFromTrimmedBottomLeft = new Vector2(sliceRect.x - trimmedBounds.x + w / 2f, sliceRect.y - trimmedBounds.y + h / 2f);
            Vector3 sliceOffset = (Vector3)(sliceCenterFromTrimmedBottomLeft / originalSprite.pixelsPerUnit);
            slicedPieceObject.transform.position = worldAnchor + originalPiece.transform.TransformVector(sliceOffset);
            
            Piece slicedPieceComponent = slicedPieceObject.GetComponent<Piece>();
            if (slicedPieceComponent != null)
            {
                slicedPieceComponent.pieceCount = 1;
            }

            slicedPieces.Add(slicedPieceObject);
        }

        // 그림자 처리 Step 3: 원본 그림자를 올바른 조각에 재부착 및 위치/스케일 복원
        if (originalShadow != null)
        {
            int targetIndex = -1;
            if (sliceDirection == SliceDirection.Vertical)
            {
                targetIndex = 0; // 제일 하단
            }
            else // Horizontal
            {
                targetIndex = (pieceCount - 1) / 2; // 중앙
            }

            if (targetIndex != -1 && slicedPieces.Count > targetIndex)
            {
                originalShadow.SetParent(slicedPieces[targetIndex].transform);
            }
            else
            {
                // 부착할 조각이 없으면 그림자도 파괴
                Destroy(originalShadow.gameObject);
            }
        }

        return slicedPieces;
    }

    /// <summary>
    /// 원본 Texture2D에서 투명(alpha)이 아닌 픽셀만 포함하는 최소 영역(Rect)을 계산하여 반환합니다.
    /// </summary>
    private static Rect GetTrimmedTextureBounds(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color[] pixels = texture.GetPixels();

        int minX = width, minY = height, maxX = 0, maxY = 0;
        bool hasContent = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a > 0)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    hasContent = true;
                }
            }
        }

        if (!hasContent)
        {
            return new Rect(0, 0, 0, 0);
        }

        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private void OnDestroy()
    {
        // 동적으로 생성된 조각(Seperated_)인 경우, 할당된 리소스를 직접 해제하여 메모리 누수를 방지합니다.
        if (gameObject.name.StartsWith("Seperated_"))
        {
            if (spriteRenderer != null && spriteRenderer.Length > 0 && spriteRenderer[0] != null)
            {
                Sprite sprite = spriteRenderer[0].sprite;
                if (sprite != null)
                {
                    if (sprite.texture != null)
                    {
                        Destroy(sprite.texture);
                    }
                    Destroy(sprite);
                }
            }
        }
    }
}