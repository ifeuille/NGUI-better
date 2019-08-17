using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenNotchAdapter : MonoBehaviour
{
    private static ScreenNotchAdapter Ins_;
    public ScreenNotchAdapter Instance
    {
        get { return Ins_; }
    }
    // Start is called before the first frame update
    private void Awake()
    {
        Ins_ = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
