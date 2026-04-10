using System;
using UnityEngine;




/// <summary>
/// 占쌤븝옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙
/// </summary>
[Serializable]
public struct SecureInt
{
    [SerializeField] private int m_obfuscatedValue; // XOR 占쏙옙호화占쏙옙 占쏙옙
    [SerializeField] private int m_checksum;        // 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 체크占쏙옙

    public int Value
    {
        get
        {
            int original = m_obfuscatedValue ^ *;
            // 占쏙옙占쏙옙 占쏙옙占쏙옙: 占쏙옙占쏙옙 占쏙옙占쏙옙占실억옙占쏙옙占쏙옙 확占쏙옙
            if (m_checksum != (original ^ *))
            {
                Custom.CustomDebug.PrintE("[Security] 占쌨몌옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙!");
                return 0; // 占쏙옙占쏙옙 占쏙옙 0 占쏙옙환 占실댐옙 占쏙옙占쏙옙 占쏙옙占쏙옙 처占쏙옙
            }
            return original;
        }
        set
        {
            m_obfuscatedValue = value ^ *;
            m_checksum = value ^ *; // 占쏙옙占쏙옙 占쏙옙占쏙옙占?체크占쏙옙 占쏙옙占쏙옙
        }
    }

    public SecureInt(int initialValue)
    {
        m_obfuscatedValue = initialValue ^ *;
        m_checksum = initialValue ^ *;
    }

    // --- ?곗궛???ㅻ쾭濡쒕뵫 ---
    public static implicit operator int(SecureInt secureInt) => secureInt.Value;
    public static implicit operator SecureInt(int i) => new SecureInt(i);

    public override string ToString() => Value.ToString();
}
