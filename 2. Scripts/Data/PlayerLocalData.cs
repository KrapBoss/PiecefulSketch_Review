using UnityEngine;
public static class PlayerLocalData
{
    private static string _nickName = null;
    public static string NickName
    {
        set => _nickName = value;
        get
        {
            if (string.IsNullOrWhiteSpace(_nickName))
            {
                var rngPlayerNumber = Random.Range(0, 9999);
                _nickName = $"Player {rngPlayerNumber.ToString("0000")}";
            }
            return _nickName;
        }
    }
}
