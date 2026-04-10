using UnityEngine;

namespace Custom
{
    public static class CustomDebug
    {
        public static void Print(string str)
        {
            Debug.Log(str);
        }
        public static void PrintW(string str)
        {
            Debug.LogWarning(str);
        }
        public static void PrintE(string str)
        {
            Debug.LogError(str);
        }

        public static void Exeption(string str) { throw new System.Exception(str); }
    }
}
