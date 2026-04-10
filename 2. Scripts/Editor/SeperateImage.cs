using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 알파 픽셀 기준으로 연결된 오브젝트를 분리한 뒤
/// Grid 방식으로 재배치된 Sprite Sheet를 생성하는 에디터 툴
/// 제작 의도: AI 생성 Sprite Sheet의 겹침 문제 자동 해결
/// Unity 효율성: 수동 Slice / 재배치 작업 제거
/// </summary>
public static class SpriteSheetRepackEditor
{
    [MenuItem("Tools/Sprite/Repack Sprite Sheet (Alpha Based)")]
    public static void RepackSelectedTexture()
    {
        Texture2D source = Selection.activeObject as Texture2D;
        if (source == null)
        {
            Debug.LogError("Texture2D를 선택하세요.");
            return;
        }

        // Read/Write 강제
        string path = AssetDatabase.GetAssetPath(source);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.isReadable = true;
        importer.SaveAndReimport();

        // 재배치 파라미터
        int cellSize = 256;
        int padding = 32;
        int outputSize = 2048;

        Texture2D output = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);

        // 투명 초기화
        Color clear = new Color(0, 0, 0, 0);
        Color[] clearPixels = new Color[outputSize * outputSize];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = clear;
        output.SetPixels(clearPixels);

        // 알파 덩어리 추출
        List<RectInt> objects = AlphaExtractor.Extract(source);

        int col = 0, row = 0;
        int maxCols = outputSize / cellSize;

        foreach (RectInt rect in objects)
        {
            int startX = col * cellSize + padding;
            int startY = outputSize - ((row + 1) * cellSize) + padding;

            for (int y = 0; y < rect.height; y++)
            {
                for (int x = 0; x < rect.width; x++)
                {
                    Color c = source.GetPixel(rect.x + x, rect.y + y);
                    if (c.a > 0f)
                        output.SetPixel(startX + x, startY + y, c);
                }
            }

            col++;
            if (col >= maxCols)
            {
                col = 0;
                row++;
            }
        }

        output.Apply();

        // 저장
        string newPath = path.Replace(".png", "_Repacked.png");
        System.IO.File.WriteAllBytes(newPath, output.EncodeToPNG());
        AssetDatabase.Refresh();

        Debug.Log("Repacked Sprite Sheet 생성 완료");
    }
}

/// <summary>
/// 알파 기준 Connected Component(Flood Fill)로
/// 개별 오브젝트의 Tight Bounding Box를 추출
/// </summary>
public static class AlphaExtractor
{
    /// <summary>
    /// 알파가 연결된 영역을 개별 Rect로 분리
    /// </summary>
    public static List<RectInt> Extract(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        bool[,] visited = new bool[w, h];
        List<RectInt> results = new List<RectInt>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (visited[x, y]) continue;
                if (tex.GetPixel(x, y).a <= 0f) continue;

                // Flood Fill
                int minX = x, maxX = x, minY = y, maxY = y;
                Stack<Vector2Int> stack = new Stack<Vector2Int>();
                stack.Push(new Vector2Int(x, y));
                visited[x, y] = true;

                while (stack.Count > 0)
                {
                    Vector2Int p = stack.Pop();

                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = p.x + dx;
                            int ny = p.y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            if (visited[nx, ny]) continue;
                            if (tex.GetPixel(nx, ny).a <= 0f) continue;

                            visited[nx, ny] = true;
                            stack.Push(new Vector2Int(nx, ny));
                        }
                    }
                }

                results.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }
        }

        return results;
    }
}



/// <summary>
/// 알파 연결 영역을 기준으로 폴리곤 형태처럼 누끼를 따서
/// 각각을 개별 PNG 이미지로 추출
/// 제작 의도: AI 생성 이미지에서 오브젝트 단위 에셋 자동 분리
/// Unity 효율성: 포토샵 수작업 제거
/// </summary>
public static class AlphaPolygonExtractorEditor
{
    [MenuItem("Tools/Sprite/Extract Alpha Objects (Polygon PNG)")]
    public static void Extract()
    {
        Texture2D source = Selection.activeObject as Texture2D;
        if (source == null)
        {
            Debug.LogError("Texture2D를 선택하세요.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(source);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.isReadable = true;
        importer.SaveAndReimport();

        List<RectInt> objects = AlphaExtractor.Extract(source);

        string dir = Path.GetDirectoryName(path);
        string baseName = Path.GetFileNameWithoutExtension(path);

        int index = 0;
        foreach (RectInt rect in objects)
        {
            Texture2D cut = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);

            // 투명 초기화
            Color clear = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[rect.width * rect.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            cut.SetPixels(pixels);

            // 알파 영역만 복사
            for (int y = 0; y < rect.height; y++)
            {
                for (int x = 0; x < rect.width; x++)
                {
                    Color c = source.GetPixel(rect.x + x, rect.y + y);
                    if (c.a > 0f)
                        cut.SetPixel(x, y, c);
                }
            }

            cut.Apply();

            string outPath = $"{dir}/{baseName}_part_{index}.png";
            File.WriteAllBytes(outPath, cut.EncodeToPNG());
            index++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"추출 완료: {index}개 PNG 생성");
    }
}
