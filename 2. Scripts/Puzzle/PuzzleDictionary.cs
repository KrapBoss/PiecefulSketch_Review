using Custom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 모든 퍼즐에 대한 정보를 저장하고 가지고 있는다.
/// 참조할 때 사용된다.
/// </summary>
public class PuzzleDictionary : MonoBehaviour
{
    #region Instance
    static PuzzleDictionary _instance;
    public static PuzzleDictionary Instance
    {
        get
        {
            if (StateManager.Instance == null) return null;
            //|| StateManager.Instance.State == LocalGameState.End
            if (_instance == null)
            {
                _instance = FindObjectOfType<PuzzleDictionary>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PuzzleDictionary");
                    _instance = go.AddComponent<PuzzleDictionary>();
                }
            }
            return _instance;
        }
    }
    #endregion

    private Dictionary<string, List<Piece>> dictionary = new Dictionary<string, List<Piece>>();

    //원하는 조각 중 하나
    public Piece GetPiece(string name)
    {
        if (!dictionary.ContainsKey(name))
        {
            return null;
        }

        Piece[] p = dictionary[name].Where(x => !x.data.Activation).ToArray();

        return p != null ? p[0] : null;
    }

    /// <summary>
    /// 퍼즐 조각을 전부 반환하는 역할
    /// </summary>
    /// <returns></returns>
    public PieceData[] GetAllPieceData()
    {
        return dictionary.SelectMany(kv => kv.Value).Select(x => x.data).ToArray();
    }


    //조각 추가
    public void AddPiece(string name, Piece data)
    {
        if (data == null)
        {
            Debug.LogError($"{name}에 해당하는 사전값이 없습니다.");
        }

        if (!dictionary.ContainsKey(name)) dictionary[name] = new List<Piece> { data };
        else dictionary[name].Add(data);

        //CustomDebug.PrintW($"{name}등록 {dictionary[name].Count} {data.name}");
    }

    //중복되지 않은 퍼즐 조각 데이터를 한개씩 반환
    public PieceData[] GetVariablePieceData()
    {
        if (dictionary.Count == 0) return null;

        return dictionary.Values.Select(x => x[0].data).ToArray();
    }

    //해당 이름을 가진 모든 퍼즐 조각을 반환합니다.
    public Piece[] GetPieceWithName(string name)
    {
        if (!dictionary.ContainsKey(name)) return null;
        return dictionary[name].ToArray();
    }

    //해당 피스에 남은 조각 수
    public int GetPieceCount(string name)
    {
        if (dictionary.Count == 0) return 0;

        if (!dictionary.ContainsKey(name)) { Debug.LogWarning($"{name} 에 해당하는 값이 없습니다."); return 0; }

        //아무것도 활성화 되지 않은 경우 null을 반환합니다.
        int? length = dictionary[name].Where(x => !x.data.Activation)?.Count();


        return length == null ? 0 : (int)length;
    }
    //모든 조각 제거
    public void Clear()
    {
        if (dictionary.Count == 0) { CustomDebug.PrintW("퍼즐 사전에 데이터가 없습니다."); return; }

        foreach (var p in dictionary.Values)
        {
            p.Clear();
        }

        dictionary.Clear();
    }

    /// <summary> 값이 존재? </summary>
    public bool IsVaild => dictionary.Count > 0;

    private void OnDestroy()
    {
        Clear();
        _instance = null;
    }
}
