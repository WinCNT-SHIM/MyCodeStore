using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GaugeController : MonoBehaviour
{
    public List<Material> GaugeList;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float fillAmount = Mathf.Abs(Mathf.Sin(Time.time));

        foreach (var gauge in GaugeList)
        {
            gauge.SetFloat("_FillAmount", fillAmount);
        }
    }
}
