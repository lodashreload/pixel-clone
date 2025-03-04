﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Dice;

public class UIScanView
    : UIPage
{
    [Header("Controls")]
    public GameObject contentRoot;
    public UIPairSelectedDiceButton pairSelectedDice;
    public Button clearListButton;

    [Header("Prefabs")]
    public UIDiscoveredDieView discoveredDiePrefab;

    List<UIDiscoveredDieView> discoveredDice = new List<UIDiscoveredDieView>();
    List<UIDiscoveredDieView> selectedDice = new List<UIDiscoveredDieView>();

    void Awake()
    {
        clearListButton.onClick.AddListener(ClearList);
    }

    void OnEnable()
    {
        base.SetupHeader(false, false, "Scanning", null);
        RefreshView();
        StartCoroutine(BeginScanCr());
    }

    IEnumerator BeginScanCr()
    {
        while (Central.Instance.state != Central.State.Idle) yield return null;
        DicePool.Instance.onDieDiscovered += OnDieDiscovered;
        DicePool.Instance.BeginScanForDice();
        pairSelectedDice.SetActive(false);
    }

    void OnDisable()
    {
        if (DicePool.Instance != null)
        {
            DicePool.Instance.StopScanForDice();
            DicePool.Instance.onDieDiscovered -= OnDieDiscovered;
        }
        foreach (var die in discoveredDice)
        {
            die.die.OnConnectionStateChanged -= OnDieStateChanged;
            die.onSelected -= OnDieSelected;
            DestroyDiscoveredDie(die);
        }
        selectedDice.Clear();
        discoveredDice.Clear();
    }

    void RefreshView()
    {
        // Assume all scanned dice will be destroyed
        var toDestroy = new List<UIDiscoveredDieView>(discoveredDice);
        foreach (var die in DicePool.Instance.allDice.Where(d =>
            d.connectionState == Die.ConnectionState.Available))
        {
            if (!DiceManager.Instance.allDice.Any(d => d.die == die || (d.deviceId != 0 && d.deviceId == die.deviceId) || d.name == die.name))
            {
                // It's an advertising die we don't *know* about
                int prevIndex = toDestroy.FindIndex(uid => uid.die == die);
                if (prevIndex == -1)
                {
                    // New scanned die
                    var newUIDie = CreateDiscoveredDie(die);
                    discoveredDice.Add(newUIDie);
                }
                else
                {
                    toDestroy.RemoveAt(prevIndex);
                }
            }
        }

        // Remove all remaining dice
        foreach (var uidie in toDestroy)
        {
            discoveredDice.Remove(uidie);
            DestroyDiscoveredDie(uidie);
        }
    }

    public override void OnBack()
    {
        NavigationManager.Instance.GoBack();
    }

    UIDiscoveredDieView CreateDiscoveredDie(Die die)
    {
        //Debug.Log("Creating discovered Die: " + die.name);
        // Create the gameObject
        var ret = GameObject.Instantiate<UIDiscoveredDieView>(discoveredDiePrefab, Vector3.zero, Quaternion.identity, contentRoot.transform);
        ret.transform.SetAsFirstSibling();
        // Initialize it
        ret.Setup(die);
        ret.onSelected += OnDieSelected;
        return ret;
    }

    void DestroyDiscoveredDie(UIDiscoveredDieView dieView)
    {
        //Debug.Log("Destroying discovered Die: " + dieView.die.name);
        GameObject.Destroy(dieView.gameObject);
    }

    void PairSelectedDice()
    {
        pairSelectedDice.onClick.RemoveListener(PairSelectedDice);
        pairSelectedDice.SetActive(false);
        var selectedDieCopy = new List<Die>(selectedDice.Select(d => d.die));
        // Tell the navigation to go back to the pool, and then start connecting to the selected dice
        DiceManager.Instance.AddDiscoveredDice(selectedDieCopy);
        NavigationManager.Instance.GoBack();
    }

    void OnDieDiscovered(Die newDie)
    {
        newDie.OnConnectionStateChanged += OnDieStateChanged;
        RefreshView();
    }

    void OnDieSelected(UIDiscoveredDieView uidie, bool selected)
    {
        if (selected)
        {
            if (selectedDice.Count == 0)
            {
                pairSelectedDice.onClick.AddListener(PairSelectedDice);
                pairSelectedDice.SetActive(true);
            }
            selectedDice.Add(uidie);
        }
        else
        {
            selectedDice.Remove(uidie);
            if (selectedDice.Count == 0)
            {
                pairSelectedDice.onClick.RemoveListener(PairSelectedDice);
                pairSelectedDice.SetActive(false);
            }
        }
    }

    // void onWillDestroyDie(Die die)
    // {
    //     var uidie = discoveredDice.Find(d => d.die == die);
    //     if (uidie != null)
    //     {
    //         die.OnConnectionStateChanged -= OnDieStateChanged;
    //         Debug.Assert(die.connectionState == Die.ConnectionState.New); // if not we should have been notified previously
    //         discoveredDice.Remove(uidie);
    //         DestroyDiscoveredDie(uidie);
    //     }
    // }

    void OnDieStateChanged(Die die, Die.ConnectionState oldState, Die.ConnectionState newState)
    {
        RefreshView();
    }

    void ClearList()
    {
        //TODO it's possible that the scan doesn't stop if it was triggered more than once
        DicePool.Instance.StopScanForDice();
        DicePool.Instance.ClearScanList();
        RefreshView();
        DicePool.Instance.BeginScanForDice();
    }
}
