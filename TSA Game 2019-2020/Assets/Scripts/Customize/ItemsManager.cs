using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemsManager : MonoBehaviour
{
    public List<GameObject> items;
    public bool applyItems; //If true, will load currently equipped items to the character

    public AccountManager accMan;
    public GameObject playerObj;

    private void Start()
    {
        if (applyItems)
            accMan.ApplyItems(PlayerPrefs.GetString("username"), this);
    }

    public void ApplyItems(AccountData data)
    {
        //Apply items here
    }
}
