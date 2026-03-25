using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private GridSystemVisual GV;
    private MoveAction MA;
    void Start()
    {

    }
    private void Update()
    {


        if(Input.GetKeyDown(KeyCode.G))
        {
            GV.HideAllGridPosition();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            GV.HideAllGridPosition();
            GV.ShowGridPositionList(unit.GetComponent<MoveAction>().GetValidGridPosition());
        }
    

    }

}
