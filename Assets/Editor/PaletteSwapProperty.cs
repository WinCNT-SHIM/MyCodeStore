using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomShaderGUI
{
    [Flags]
    public enum PaletteSwapMode
    {
        Linear = 1,
        Multiply = 2,
        Screen = 4,
        Overlay = 8
    }
    
    internal static class PaletteSwapProperty
    {
        public static readonly string BaseColor = "_BaseColor";
        public static readonly string BaseMap = "_BaseMap";
        public static readonly string PaletteSwap = "_PaletteSwap";
        public static readonly string PaletteSwapMask = "_PaletteSwapMask";
        public static readonly string PaletteSwapMask1Color = "_PaletteSwapMask1Color";
        public static readonly string PaletteSwapMask2Color = "_PaletteSwapMask2Color";
        public static readonly string PaletteSwapMask3Color = "_PaletteSwapMask3Color";
        public static readonly string PaletteSwapMask1ColorMode = "_PaletteSwapMask1ColorMode";
        public static readonly string PaletteSwapMask2ColorMode = "_PaletteSwapMask2ColorMode";
        public static readonly string PaletteSwapMask3ColorMode = "_PaletteSwapMask3ColorMode";
    }
}