using App.Battle.Views;
using UnityEngine;

public class NumberController : MonoBehaviour
{
    public DamagePopupView damagePopupView;
    public float number;
    public Vector3 position;
    public float hue;

    // Update is called once per frame
    void Update()
    {
        if (damagePopupView == null)
            return;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            damagePopupView.Add(number, position, hue);
        }
    }
}
