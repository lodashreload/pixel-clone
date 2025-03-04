﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Animations
{
    [System.Serializable]
    public class EditAnimationSimple
        : EditAnimation
    {
        [Slider, FloatRange(0.1f, 10.0f, 0.1f), Units("sec")]
        public override float duration { get; set; }
        [FaceMask, IntRange(0, 19), Name("Face Mask")]
		public int faces = 0xFFFFF;
        public EditColor color = EditColor.MakeRGB(new Color32(0xFF, 0x30, 0x00, 0xff));
        [Index, IntRange(1, 10), Name("Repeat Count")]
        public int count = 1;
        [Slider]
        [FloatRange(0.1f, 1.0f), Name("Fading Sharpness")]
        public float fade = 0.1f;

        public override AnimationType type { get { return AnimationType.Simple; } }
        public override IAnimation ToAnimation(EditDataSet editSet, DataSet.AnimationBits bits)
        {
            var ret = new AnimationSimple();
            ret.duration = (ushort)(this.duration * 1000.0f);
            ret.faceMask = (uint)this.faces;
            ret.colorIndex = (ushort)color.toColorIndex(ref bits.palette);
            ret.fade = (byte)(255.0f * fade);
            ret.count = (byte)count;
            return ret;
        }
 
        public override EditAnimation Duplicate()
        {
            EditAnimationSimple ret = new EditAnimationSimple();
            ret.name = this.name;
		    ret.duration = this.duration;
            ret.faces = this.faces;
            ret.color = this.color;
            ret.count = this.count;
            return ret;
        }
   }
}