using Custom;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 퍼즐 조각의 틀
/// 퍼즐의 완성
/// </summary>
public class PuzzleContainer : MonoBehaviour
{
    static PuzzleContainer _puzzleContainer;
    public static PuzzleContainer Container
    {
        get
        {
            if (_puzzleContainer == null)
            {
                _puzzleContainer = FindAnyObjectByType<PuzzleContainer>();
            }
            if (_puzzleContainer == null)
            {
                CustomDebug.PrintE("Not Vaild Puzzle Container in Scene");
            }

            return _puzzleContainer;
        }
    }

    [SerializeField] Piece BackGround; // 백그라운드 퍼즐 조각은 분할되지 않아야합니다.

    //이미지를 캡쳐를 진행하기 위함입니다.
    Capture2D capture;

    //퍼즐 완성 수치
    private float Perfection = 0;
    //총 퍼즐 조각 수
    private int CountPiece;

    //퍼즐이 맞았을 때 외부로 알리는 액션
    public Action<float> Action_Fit;

    public Sprite BackGroundSprite
    {
        get
        {
            if (BackGround.data.Sprite == null)
            {
                BackGround.Initialize(this);
            }
            return BackGround.data.Sprite;
        }
    }//백그라운드 스프라이트를 반환

    public Piece BackgroundPiece => BackGround;

    //퍼즐 컨테이너는 시작과 동시에 초기화를 진행합니다.
    public void Initialize()
    {
        Piece.ResetSeparatedCounter();

        CustomDebug.PrintW($"PuzzleContainer {transform.name} Awake");

        PuzzleDictionary.Instance.Clear();

        transform.position = Vector3.zero;

        //퍼즐 등록
        RegisterPiece();

        BackGround.PieceFit();

        Perfection = 0;
    }

    //퍼즐 조각을 최종적으로 등록합니다.
    void RegisterPiece()
    {
        // 1. 씬에 배치된 원본 조각들을 가져옵니다.
        Piece[] initialPieces = GetComponentsInChildren<Piece>();

        if (initialPieces.Length < 1) { Debug.LogWarning($"{name} 에 포함된 퍼즐 조각이 없습니다."); return; }

        // 2. 이미지 캡처를 먼저 수행합니다.
        capture = FindObjectOfType<Capture2D>();
        capture.Capture(BackGround.transform, true);

        // 3. 최종적으로 등록될 조각들을 담을 리스트를 생성합니다.
        List<Piece> finalPieces = new List<Piece>();

        // 4. 원본 조각들을 순회하며 분할 작업을 수행합니다.
        foreach (Piece piece in initialPieces)
        {
            if (piece == BackGround)
            {
                // 백그라운드 조각은 그대로 등록 리스트에 추가합니다.
                finalPieces.Add(piece);
                continue;
            }

            if (piece.pieceCount > 1)
            {
                // 분할이 필요한 경우, Slice 함수를 호출합니다.
                List<GameObject> slicedObjects = Piece.Slice(piece.gameObject);
                
                // 슬라이스가 성공했는지 확인합니다.
                if (slicedObjects != null && slicedObjects.Count > 0)
                {
                    // 성공 시: 분할된 조각들을 최종 리스트에 추가합니다.
                    foreach (var obj in slicedObjects)
                    {
                        finalPieces.Add(obj.GetComponent<Piece>());
                    }
                    // 원본 조각은 파괴합니다.
                    Destroy(piece.gameObject);
                }
                else
                {
                    // 실패 시: 원본 조각을 최종 리스트에 추가합니다.
                    finalPieces.Add(piece);
                }
            }
            else
            {
                // 분할이 필요 없는 경우, 바로 최종 리스트에 추가합니다.
                finalPieces.Add(piece);
            }
        }

        // 5. 최종 조각 리스트를 기준으로 초기화 및 등록을 진행합니다.
        CountPiece = 0;
        foreach (Piece data in finalPieces)
        {
            // 각 조각을 초기화합니다.
            data.Initialize(this);

            // 백그라운드 조각은 등록 과정에서 제외합니다.
            if (data == BackGround) continue;

            CountPiece++;
            PuzzleDictionary.Instance.AddPiece(data.data.PieceName, data);
        }
    }

    //퍼즐 조각을 맞춘 개수를 전달받아 퍼즐의 완성도를 계산하여 넘깁니다.
    public void FitThePiece()
    {
        Perfection++;
        //퍼즐 완성도를 외부에 전달합니다.-------------------------------
        if (Action_Fit != null) Action_Fit(Perfection / CountPiece);

        Debug.Log($"퍼즐 조각 {Perfection}/{CountPiece}");

        Managers.AdsManager.Instance.CheckAD(null);
    }

    /// <summary> 진행도 </summary>
    public float GetProgress => Perfection / CountPiece;

    private void OnDestroy()
    {
        //퍼즐 컨테이너가 꺼지면 비웁니다.
        if (PuzzleDictionary.Instance) PuzzleDictionary.Instance.Clear();

        Action_Fit = null;
    }
}
