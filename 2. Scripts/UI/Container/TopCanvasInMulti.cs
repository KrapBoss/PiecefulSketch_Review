using UnityEngine;

public class TopCanvasInMulti : MonoBehaviour
{
    public static TopCanvasInMulti Instance { get; private set; }

    [SerializeField] Blind blind;
    public Blind Blind { get { return blind; } }


    [SerializeField] ResultMulti result;
    public ResultMulti Result { get { return result; } }

    private void Awake()
    {
        Instance = this;

        blind.gameObject.SetActive(false);
        result.gameObject.SetActive(false);
    }
}
