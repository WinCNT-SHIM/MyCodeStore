using UnityEngine;

public class VignetteController : MonoBehaviour
{
    public Camera camera;
    public Vignette vignette;
    
    // Start is called before the first frame update
    void Start()
    {
        vignette.Init(camera);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            vignette.VignetteIn();
        }
        else
        {
            vignette.VignetteOut();
        }
    }
}
