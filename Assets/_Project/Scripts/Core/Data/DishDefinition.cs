using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>How much of which dirt type a dish carries when it arrives dirty.</summary>
    [Serializable]
    public struct DirtLayer
    {
        public DirtTypeDefinition dirtType;
        [Range(0f, 1f)] public float coverage;
    }

    /// <summary>Procedural mesh used for a dish (all share the same footprint: radius 0.5, height 2).</summary>
    public enum DishShape
    {
        Cylinder,
        Plate,
        Pan,
        Glass,
        Bowl,
        Pot,
        Mug,
        Roaster
    }

    /// <summary>What a dish is made of: how it breaks (china and glass shatter, steel only rings), slides and sounds.</summary>
    public enum DishMaterial
    {
        Ceramic,
        Glass,
        Steel
    }

    /// <summary>A washable item: how it looks (procedural shape + material) and what dirt it starts with.</summary>
    [CreateAssetMenu(menuName = "Zmywak/Dish Definition", fileName = "Dish_")]
    public class DishDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Cylinder-based dishes use the procedural Shape mesh; anything else falls back to a Unity primitive.")]
        [SerializeField] private PrimitiveType placeholderPrimitive = PrimitiveType.Cube;
        [SerializeField] private DishShape shape = DishShape.Cylinder;
        [SerializeField] private Vector3 placeholderScale = Vector3.one;
        [Tooltip("Colour of the dish once it is clean (white china, steel pan, ...).")]
        [SerializeField] private Color placeholderCleanColor = Color.white;
        [Tooltip("Gloss of the clean surface (dirt is always matte).")]
        [SerializeField, Range(0f, 1f)] private float smoothness = 0.8f;
        [SerializeField, Range(0f, 1f)] private float metallic;
        [Tooltip("Above 0 the dish is see-through glass: how opaque it is when clean (dirt clouds it). 0 = glazed china or metal.")]
        [SerializeField, Range(0f, 1f)] private float cleanOpacity;
        [SerializeField] private List<DirtLayer> dirtLayers = new List<DirtLayer>();

        [Header("Rack")]
        [Tooltip("Rack cells the dish covers: x along its handle side, y across - before any quarter turn.")]
        [SerializeField] private Vector2Int rackFootprint = Vector2Int.one;

        [Tooltip("Where the dish body sits relative to the centre of its footprint, in cells (a pan's handle sticks out to one side).")]
        [SerializeField] private Vector2 rackBodyOffset;

        [Header("Leftovers")]
        [Tooltip("The scraps on it when it comes in dirty: a heap about one unit across on y = 0 (scaled to the dish), " +
                 "modelled in Blender (Tools/Blender/leftovers.py). None: generic coloured lumps.")]
        [SerializeField] private Mesh leftoversMesh;
        [Tooltip("The scraps' palette: a column per food, shaded from dark (occluded) at the bottom to lit at the top.")]
        [SerializeField] private Texture2D leftoversPalette;

        [Header("Physics")]
        [Tooltip("What it is made of: whether a hard knock breaks it, how it slides on the pile, how it rings.")]
        [SerializeField] private DishMaterial material = DishMaterial.Ceramic;
        [Tooltip("Its weight in the pile: a steel pot shoves a glass aside, a glass does not shove a pot.")]
        [SerializeField, Min(0.05f)] private float massKg = 0.5f;

        public string Id => id;
        public string DisplayName => displayName;
        public PrimitiveType PlaceholderPrimitive => placeholderPrimitive;
        public DishShape Shape => shape;
        public float Smoothness => smoothness;
        public float Metallic => metallic;
        public float CleanOpacity => cleanOpacity;
        public bool SeeThrough => cleanOpacity > 0f;
        public Vector3 PlaceholderScale => placeholderScale;
        public Color PlaceholderCleanColor => placeholderCleanColor;
        public IReadOnlyList<DirtLayer> DirtLayers => dirtLayers;
        public Vector2Int RackFootprint => rackFootprint;
        public Vector2 RackBodyOffset => rackBodyOffset;
        public Mesh LeftoversMesh => leftoversMesh;
        public Texture2D LeftoversPalette => leftoversPalette;
        public DishMaterial Material => material;
        public float MassKg => massKg;
    }
}
