using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers{

    public class CanvasManager : MonoBehaviour
    {
        static CanvasManager instance;
        public static CanvasManager Instance;

        private void Awake()
        {
            instance = this;
        }
    }
}
