using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 6(6000.3.1f1) 환경에서 Puzzle_Live 하위 폴더의 신규 이미지 설정을 자동화합니다.
/// </summary>
public class PuzzleLiveTextureImporter : AssetPostprocessor
{
    /// <summary>
    /// 텍스처 임포트 직전 실행되어 이미지 설정을 변경합니다.
    /// </summary>
    void OnPreprocessTexture()
    {
        // 1. 경로 검사: Puzzle_Live/자식폴더/... 형태인지 확인
        if (!IsInTargetSubFolder(assetPath)) return;

        // 2. 신규 파일 검증: 기존 에셋의 설정을 유지하기 위해 메타 데이터가 없는 경우에만 실행
        // 제작 의도: 기존 작업 내역을 보호하고 신규 리소스에 대해서만 컨벤션을 강제함.
        if (!assetImporter.importSettingsMissing) return;

        TextureImporter importer = (TextureImporter)assetImporter;

        // 3. TextureImporterSettings를 이용한 세부 설정 (오류 해결 지점)
        // Unity 효율성: 직접 참조 대신 Settings 객체를 통해 구조적으로 데이터를 수정하여 API 호환성 확보.
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = 200.0f;
        settings.spriteMeshType = SpriteMeshType.Tight; // meshType -> spriteMeshType으로 수정
        settings.spriteExtrude = 1;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteGenerateFallbackPhysicsShape = true; // Generate Physics Shape

        importer.SetTextureSettings(settings);

        // 4. 공통 속성 및 플랫폼별 최적화 적용
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;

        ApplyPlatformOverride(importer, "Android");
        ApplyPlatformOverride(importer, "iPhone");
    }

    /// <summary>
    /// 대상 폴더 구조(Puzzle_Live/자식폴더/...) 내에 있는지 확인합니다.
    /// </summary>
    private bool IsInTargetSubFolder(string path)
    {
        string targetRoot = "Assets/3. Resources/Atlas/1.Puzzle_Live/";
        if (!path.StartsWith(targetRoot)) return false;

        string subPath = path.Substring(targetRoot.Length);
        return subPath.Contains("/");
    }

    /// <summary>
    /// Android 및 iOS 플랫폼 전용 설정을 적용합니다. (128, Mitchell, RGBA 32bit)
    /// </summary>
    private void ApplyPlatformOverride(TextureImporter importer, string platform)
    {
        // 제작 의도: 플랫폼별 빌드 용량 최적화 및 32비트 퀄리티 유지.
        TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platform);

        platformSettings.overridden = true;
        platformSettings.maxTextureSize = 128;
        platformSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        platformSettings.format = TextureImporterFormat.RGBA32;

        importer.SetPlatformTextureSettings(platformSettings);
    }
}