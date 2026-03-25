using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;


    public void Hide()
    {
        meshRenderer.enabled=false;
    }
    public void Show()
    {
        meshRenderer.enabled = true;
    }
}
