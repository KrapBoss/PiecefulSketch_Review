using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PuzzleSortContainer))]
public class PuzzleContainerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PuzzleSortContainer container = (PuzzleSortContainer)target;
        if (GUILayout.Button("SortALLData"))
        {
            container.SortAllPuzzleData();
        }
        if (GUILayout.Button("ChildDataInitialize"))
        {
            container.Initialize();
        }
        if (GUILayout.Button("Counting Puzzle Piece"))
        {
            container.CountingPuzzle();
        }
        //if (GUILayout.Button("MakeAllNetworkPiece"))
        //{
        //    container.MakeNetworkPiece();
        //}
    }
}
