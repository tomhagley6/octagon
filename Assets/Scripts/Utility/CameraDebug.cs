public class CameraDebug : MonoBehaviour
{
    void Update()
    {
        if (!GetComponent<Camera>().enabled)
        {
            Debug.LogWarning($"{gameObject.name} Camera disabled at frame {Time.frameCount}");
        }
    }
}