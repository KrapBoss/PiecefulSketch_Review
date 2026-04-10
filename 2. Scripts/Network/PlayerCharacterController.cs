using Fusion;
using UnityEngine;

public class PlayerCharacterController : NetworkBehaviour
{
    //보간비율
    public float t;

    Transform _target;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _target = FindObjectOfType<CameraController>().transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (_target == null) return;

            transform.position = Vector2.Lerp(transform.position, _target.position, t * Runner.DeltaTime);
        }
    }
}
