using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraEventKiller : MonoBehaviour
{
    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Set EventMask về 0 (Nothing). 
            // Camera sẽ hoàn toàn bỏ qua việc tính toán SendMouseEvents.
            cam.eventMask = 0; 
        }
    }
}