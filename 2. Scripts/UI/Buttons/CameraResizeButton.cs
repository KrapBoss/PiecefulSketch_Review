using Custom;
using UnityEngine;

public class CameraResizeButton : MonoBehaviour, IOnOff
{
    public bool Active { get; set; }

    public bool Off()
    {
        CustomDebug.PrintW("카메라 리 사이징");

        //항상 Off 를 발동 시키키 위함
        Active = true;

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ResizeCamera(true);
        }

        return true;
    }

    public bool On()
    {
        return false;
    }
}
