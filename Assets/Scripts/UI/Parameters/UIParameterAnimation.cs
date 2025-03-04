﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Animations;
using Dice;

public class UIParameterAnimation
    : UIParameter
{
    [Header("Controls")]
    public Text nameText;
    public Text animationNameText;
    public RawImage animationRender;
    public Button selectAnimationButton;

    public SingleDiceRenderer dieRenderer { get; private set; }

    public override bool CanEdit(System.Type parameterType, IEnumerable<object> attributes = null)
    {
        return parameterType == typeof(Animations.EditAnimation);
    }

    void OnDestroy()
    {
        if (DiceRendererManager.Instance != null && this.dieRenderer != null)
        {
            DiceRendererManager.Instance.DestroyDiceRenderer(this.dieRenderer);
            this.dieRenderer = null;
        }
    }

    protected override void SetupControls(string name, System.Func<object> getterFunc, System.Action<object> setterAction, IEnumerable<object> attributes = null)
    {
        EditAnimation initialAnim = (EditAnimation)getterFunc();
        
        // Set name
        nameText.text = name;

        var design = DesignAndColor.V5_Grey;
        if (initialAnim != null)
        {
            design = initialAnim.defaultPreviewSettings.design;
        }
        this.dieRenderer = DiceRendererManager.Instance.CreateDiceRenderer(design, 160);
        if (dieRenderer != null)
        {
            animationRender.texture = dieRenderer.renderTexture;
        }

        selectAnimationButton.onClick.AddListener(() => PixelsApp.Instance.ShowAnimationPicker("Select Lighting Pattern", (EditAnimation)getterFunc.Invoke(), (res, newAnim) => 
        {
            if (res)
            {
                SetAnimation(newAnim);
                setterAction?.Invoke((EditAnimation)newAnim);
            }
        }));

        // Set animation name field
        if (initialAnim != null)
        {
            dieRenderer.SetAnimation(initialAnim);
            dieRenderer.Play(true);
            animationNameText.text = initialAnim.name;
            dieRenderer.SetAuto(true);
        }
        else
        {
            dieRenderer.SetAuto(false);
        }
    }

    void SetAnimation(EditAnimation newAnimation)
    {
        if (newAnimation != null)
        {
            dieRenderer.SetAuto(true);
            dieRenderer.SetAnimation(newAnimation);
            animationNameText.text = newAnimation.name;
        }
        else
        {
            dieRenderer.ClearAnimations();
            dieRenderer.SetAuto(false);
            animationNameText.text = "- Please select a Pattern -";
        }
    }
}
