using Custom;
using UnityEngine;

public class Capture2D : MonoBehaviour
{
    [SerializeField] private Camera renderCamera;

    public Sprite Picture { get; private set; }

    /// <summary>
    /// 대상 오브젝트를 투명 배경으로 캡쳐하여 Texture2D로 반환합니다.
    /// </summary>
    /// <param name="boundObject">캡쳐 대상 Transform</param>
    /// <param name="Save">Sprite 변수에 저장 여부</param>
    /// <returns>알파 채널이 포함된 Texture2D</returns>
    public Texture2D Capture(Transform boundObject, bool Save)
    {
        CustomDebug.PrintW("투명 배경 이미지 캡쳐를 시작합니다.");

        // 1. 카메라 기본 설정 (투명 배경 핵심)
        renderCamera.enabled = true;
        renderCamera.orthographic = true;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0); // 배경을 완전히 투명하게 설정

        // 2. 영역 및 위치 설정
        Bounds bounds = CalculateBounds(boundObject);
        renderCamera.orthographicSize = bounds.extents.y;
        renderCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, renderCamera.transform.position.z);

        // 3. RenderTexture 생성 (알파 채널 지원 포맷 필수)
        int textureWidth = (int)(bounds.size.x * 100);
        int textureHeight = (int)(bounds.size.y * 100);

        // RenderTextureFormat.ARGB32 사용하여 알파 지원
        RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
        renderCamera.targetTexture = renderTexture;

        // 4. 렌더링 및 픽셀 읽기
        renderCamera.Render();
        RenderTexture.active = renderTexture;

        // TextureFormat.RGBA32 사용하여 투명도 포함
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        texture.Apply();

        // 5. 메모리 정리
        renderCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        renderCamera.enabled = false;

        if (Save)
        {
            // Sprite 생성 시에도 투명도가 유지됨
            Picture = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            Picture.name = boundObject.name;
        }

        Resources.UnloadUnusedAssets();
        return texture;
    }

    /// <summary>
    /// 객체의 Renderer를 기반으로 전체 경계 영역을 계산합니다.
    /// </summary>
    private Bounds CalculateBounds(Transform obj)
    {
        if (obj == null) return new Bounds();

        Renderer targetRenderer = obj.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            return targetRenderer.bounds;
        }

        return new Bounds(obj.position, Vector3.zero);
    }
}