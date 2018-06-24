using UnityEngine;
using UnityEngine.UI;

namespace Demo.Behaviours
{
    public class FPSDisplay : MonoBehaviour
    {
        Text text;
        float deltaTime = 0.0f;
        float msec;
        float fps;
        float nextUpdate;

        void Start ()
        {
            text = GetComponent<Text> ();
        }

        void Update ()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            msec = deltaTime * 1000.0f;
            fps = 1.0f / deltaTime;
            if (Time.time > nextUpdate)
            {
                nextUpdate = Time.time + 0.5f;
                text.text = string.Format ("FPS: {0:0.0} ms ({1:0.} fps)", msec, fps);
            }
        }
    }
}