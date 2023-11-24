using App.Common.Views;
using UnityEngine;

public class ScreenFadeController : MonoBehaviour
{
    public ScreenFadeView screenFadeView;
    
    // Start is called before the first frame update
    void Start()
    {
        screenFadeView.FadeIn();
    }
}
