using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI ִϸ̼   θ
/// </summary>
public abstract class UIAnimationParent : MonoBehaviour
{
    public float duration = 0.5f;

    [SerializeField]protected UIAnimationParent m_next;

    public abstract void Show();

    public abstract void Hide();
}