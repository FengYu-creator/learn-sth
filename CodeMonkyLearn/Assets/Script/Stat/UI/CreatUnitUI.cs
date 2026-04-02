using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreatUnitUI : MonoBehaviour
{
    [SerializeField] private Transform creatPrefab;
    [SerializeField] private Transform creatContainerTransform;
    [SerializeField] private Button remakeButton;
    private Transform card1;
    private Transform card2;
    private Transform card3;
    private bool needWait = false;
    private bool isPanelOpen = false;

    private void Awake()
    {
        remakeButton.onClick.AddListener(OnClickRemake);
        StatManager.Instance.OnClickCreat += StatManager_OnClickCreate;
    }
    void Start()
    {
        remakeButton.gameObject.SetActive(false);
    
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            if (!isPanelOpen)
            {
                Create3Card();
                remakeButton.gameObject.SetActive(true);
                creatContainerTransform.gameObject.SetActive(true);
                isPanelOpen = true;
            }
            else
            {
                creatContainerTransform.gameObject.SetActive(false);
                remakeButton.gameObject.SetActive(false);
                isPanelOpen = false;
            }

        }
    }
    public void Create3Card()
    {
         card1 = Instantiate(creatPrefab, creatContainerTransform);
         card2 = Instantiate(creatPrefab, creatContainerTransform);
         card3 = Instantiate(creatPrefab, creatContainerTransform);
    }
    public void Remove3Card()
    {
        Destroy(card1.gameObject);
        Destroy(card2.gameObject);
        Destroy(card3.gameObject);
    }
    public void OnClickRemake()
    {
        if (!needWait)
        {
            needWait = true;
            Remove3Card();
            StartCoroutine(remake());
        }
    }
    IEnumerator remake()
    {
        yield return null;
        Create3Card();
        yield return new WaitForSeconds(1);
        needWait = false;
    }
    public void StatManager_OnClickCreate(object sender,EventArgs e)
    {
        remakeButton.gameObject.SetActive(false);
        Remove3Card() ;
        creatContainerTransform.gameObject.SetActive(false);
        isPanelOpen = false;
    }
}
