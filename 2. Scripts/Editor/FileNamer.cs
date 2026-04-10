using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// 파일의 인덱스 공백을 메우고 재정렬하며, 텍스쳐 설정을 자동 적용하는 도구입니다.
/// </summary>
public class FileRenameTool : EditorWindow
{
    private DefaultAsset targetFolder;
    private string baseName = "Puzzle";

    [MenuItem("Tools/Puzzle/Batch File Renamer")]
    public static void ShowWindow() => GetWindow<FileRenameTool>("File Renamer");

    private void OnGUI()
    {
        GUILayout.Label("Smart Reorder & Texture Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Target Folder", targetFolder, typeof(DefaultAsset), false);

        if (EditorGUI.EndChangeCheck() && targetFolder != null)
        {
            UpdateBaseNameFromFolder();
        }

        baseName = EditorGUILayout.TextField("Base Name", baseName);

        EditorGUILayout.Space();

        if (GUILayout.Button("재정렬 및 설정 실행"))
        {
            RenameAssetsSmart();
        }
    }

    /// <summary>
    /// 폴더 선택 시 폴더명을 기본 이름으로 설정합니다.
    /// </summary>
    private void UpdateBaseNameFromFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (!string.IsNullOrEmpty(folderPath))
        {
            baseName = Path.GetFileName(folderPath);
        }
    }

    /// <summary>
    /// 1. 기존 파일 번호순 정렬 2. 빈 번호 메우기 재정렬 3. 신규 파일 이름 부여 순으로 실행합니다.
    /// </summary>
    private void RenameAssetsSmart()
    {
        if (targetFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "폴더를 설정해주세요.", "확인");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        // 파일 정보를 담기 위한 임시 클래스
        var assetList = guids.Select(guid => {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            Match match = Regex.Match(name, $"^{Regex.Escape(baseName)}_(\\d+)$");

            return new
            {
                Path = path,
                IsMatched = match.Success,
                Index = match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue,
                Extension = Path.GetExtension(path)
            };
        }).ToList();

        // 정렬 로직: 
        // 1. 이름 규칙에 맞는 파일을 인덱스 순으로 정렬
        // 2. 규칙에 맞지 않는 파일(신규)은 뒤로 배치
        var sortedList = assetList
            .OrderBy(a => a.IsMatched ? 0 : 1)
            .ThenBy(a => a.Index)
            .ThenBy(a => a.Path)
            .ToList();

        AssetDatabase.StartAssetEditing();
        int changeCount = 0;

        // 이름 충돌 방지를 위해 임시 이름으로 먼저 변경 (예: Puzzle_1 -> Temp_Puzzle_1)
        // 이는 Puzzle_3을 Puzzle_2로 바꿀 때 Puzzle_2가 이미 존재할 경우를 대비함입니다.
        for (int i = 0; i < sortedList.Count; i++)
        {
            string tempName = $"TEMP_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            AssetDatabase.RenameAsset(sortedList[i].Path, tempName);
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();

        // 실제 재색인 시작
        AssetDatabase.StartAssetEditing();

        // 다시 갱신된 경로들을 가져와서 실제 이름 부여
        string[] tempGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        var tempAssets = tempGuids.Select(g => AssetDatabase.GUIDToAssetPath(g))
                                  .OrderBy(p => Path.GetFileNameWithoutExtension(p).StartsWith("TEMP_") ? 0 : 1)
                                  .ToList();

        for (int i = 0; i < sortedList.Count; i++)
        {
            string currentTempPath = tempAssets[i];
            string newFileName = $"{baseName}_{i + 1}";

            string result = AssetDatabase.RenameAsset(currentTempPath, newFileName);
            if (string.IsNullOrEmpty(result))
            {
                string directory = Path.GetDirectoryName(currentTempPath);
                string finalPath = Path.Combine(directory, newFileName + sortedList[i].Extension).Replace("\\", "/");
                ApplyTextureSettings(finalPath);
                changeCount++;
            }
        }

        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", $"{changeCount}개의 파일이 재정렬 및 설정 완료되었습니다.", "확인");
    }

    /// <summary>
    /// 텍스쳐 에셋의 Sprite Mode 및 Android/iOS 오버라이드 설정을 적용합니다. (Unity 2022+ 호환)
    /// </summary>
    /// <param name="assetPath">에셋의 상대 경로</param>
    private void ApplyTextureSettings(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        string[] platforms = { "Android", "iPhone" };

        foreach (string platform in platforms)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.maxTextureSize = 128;
            settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            settings.format = TextureImporterFormat.RGBA32;

            importer.SetPlatformTextureSettings(settings);
        }

        importer.SaveAndReimport();
    }
}