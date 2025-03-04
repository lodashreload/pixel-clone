﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Behaviors;
using System.Linq;
using Dice;

public class UIHomeBehaviorToken : MonoBehaviour
{
    [Header("Controls")]
    public Button mainButton;
    public RawImage behaviorRenderImage;
    public Text behaviorNameText;
    public Image backgroundImage;
    public Transform connectedDieRoot;

    [Header("Prefabs")]
    public UIHomeConnectedDieToken connectedDiePrefab;

    public EditBehavior editBehavior { get; private set; }
    public SingleDiceRenderer dieRenderer { get; private set; }

    public Button.ButtonClickedEvent onClick => mainButton.onClick;

    List<UIHomeConnectedDieToken> connectedDice = new List<UIHomeConnectedDieToken>();

    bool visible = true;

    public void Setup(EditBehavior behavior)
    {
        this.editBehavior = behavior;
        this.dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(behavior.defaultPreviewSettings.design);
        if (dieRenderer != null)
        {
            behaviorRenderImage.texture = dieRenderer.renderTexture;
        }
        behaviorNameText.text = behavior.name;

        dieRenderer.SetAuto(true);
        dieRenderer.SetAnimations(this.editBehavior.CollectAnimations());
        dieRenderer.Play(true);
        RefreshState();
    }

    public void RefreshState()
    {
        // Displays an icon for each die with this profile activated
        //var toDestroy = new List<UIHomeConnectedDieToken>(connectedDice);
        //foreach (var die in AppDataSet.Instance.dice)
        //{
        //    if (die.currentBehavior == editBehavior)
        //    {
        //        int prevIndex = toDestroy.FindIndex(uidie => uidie.editDie == die);
        //        if (prevIndex == -1)
        //        {
        //            // New connected die
        //            var token = CreateConnectedDieToken(die);
        //            connectedDice.Add(token);
        //        }
        //        else
        //        {
        //            toDestroy.RemoveAt(prevIndex);
        //        }
        //    }
        //}

        //// Remove remaining
        //foreach (var uidie in toDestroy)
        //{
        //    DestroyDieToken(uidie);
        //    connectedDice.Remove(uidie);
        //}
    }

    UIHomeConnectedDieToken CreateConnectedDieToken(Dice.EditDie die)
    {
        var ret = GameObject.Instantiate<UIHomeConnectedDieToken>(connectedDiePrefab, Vector3.zero, Quaternion.identity, connectedDieRoot.transform);
        ret.Setup(die);
        return ret;
    }

    void DestroyDieToken(UIHomeConnectedDieToken uidie)
    {
        GameObject.Destroy(uidie.gameObject);
    }

    void OnDestroy()
    {
        connectedDice.Clear();
        if (this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
        }
    }

    void Update()
    {
        bool newVisible = GetComponent<RectTransform>().IsVisibleFrom();
        if (newVisible != visible)
        {
            visible = newVisible;
            DiceRendererManager.Instance.OnDiceRendererVisible(dieRenderer, visible);
        }
    }

}
