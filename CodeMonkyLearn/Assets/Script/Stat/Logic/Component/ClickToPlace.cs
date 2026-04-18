using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToPlace : MonoBehaviour
{
    private Vector3 PlacePoisition;
    private Renderer renderer;
    // Start is called before the first frame update
    void Start()
    {
        PlacePoisition = transform.position;
        renderer = GetComponent<Renderer>();
        Events.BattleStarted += ClickToPlace_BattleStarted;

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit hit, float.MaxValue);
            if (EventSystem.current.IsPointerOverGameObject()) return;//UI遮挡不执行
            if (hit.transform.gameObject == this.gameObject)
            {
                renderer.material.color = Color.green;
                StatManager.Instance.onClickToPlacePosition();
                StatManager.Instance.SetPlacePosition(PlacePoisition);
            }
            else if(hit.transform.gameObject.TryGetComponent<ClickToPlace>(out ClickToPlace c))
            {
                renderer.material.color = Color.white;
            }
            else
            {
                return;
            }
        }
    }
    private void ClickToPlace_BattleStarted()
    {
        Destroy(this.gameObject);
    }
    private void OnDestroy()
    {
        Events.BattleStarted -= ClickToPlace_BattleStarted;
    }

}
