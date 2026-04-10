using UnityEngine;

/// <summary>
/// 메모리 변조를 방지하기 위해 float 값을 암호화하여 저장하는 구조체입니다.
/// </summary>
[System.Serializable]
public struct SecureFloat
{
    // 암호화에 사용될 정적 키 (변경 가능)
    private static readonly int cryptoKey = *;

    // 실제 값이 아닌, 암호화되어 저장될 값
    [SerializeField]
    private int encryptedValue;

    // 메모리 변조 감지를 위한 체크용 해시값
    [SerializeField]
    private int checksum;

    /// <summary>
    /// 실제 float 값을 가져오거나 설정합니다.
    /// </summary>
    public float Value
    {
        get
        {
            // 메모리에서 직접 값을 읽을 때 복호화하여 반환
            int decrypted = encryptedValue ^ cryptoKey;
            
            // 저장된 해시값과 현재 값으로 만든 해시값이 다르면 변조된 것으로 간주
            if (ComputeChecksum(decrypted) != checksum)
            {
                Debug.LogError("SecureFloat 값이 메모리에서 변조되었습니다!");
                return 0;
            }
            return System.BitConverter.ToSingle(System.BitConverter.GetBytes(decrypted), 0);
        }
        set
        {
            // 값을 할당할 때 암호화하여 저장
            int asInt = System.BitConverter.ToInt32(System.BitConverter.GetBytes(value), 0);
            encryptedValue = asInt ^ cryptoKey;
            checksum = ComputeChecksum(asInt);
        }
    }

    /// <summary>
    /// 생성자
    /// </summary>
    public SecureFloat(float value = 0)
    {
        encryptedValue = 0;
        checksum = 0;
        Value = value;
    }

    /// <summary>
    /// 데이터 무결성 검증을 위한 체크섬을 계산합니다.
    /// </summary>
    private int ComputeChecksum(int value)
    {
        // 간단한 해시 함수 (XOR와 시프트 연산)
        return value ^ (value >> *);
    }
    
    // --- 연산자 오버로딩 ---
    // SecureFloat을 일반 float처럼 사용할 수 있게 해줍니다.

    public override string ToString() => Value.ToString();

    // 명시적/암시적 형변환
    public static implicit operator float(SecureFloat secureFloat) => secureFloat.Value;
    public static implicit operator SecureFloat(float f) => new SecureFloat(f);

    // 산술 연산자
    public static SecureFloat operator ++(SecureFloat f) { f.Value++; return f; }
    public static SecureFloat operator --(SecureFloat f) { f.Value--; return f; }
}
