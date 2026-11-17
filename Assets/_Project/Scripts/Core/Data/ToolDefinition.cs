using System.Collections.Generic;
using UnityEngine;

namespace Zmywak.Core.Data
{
    /// <summary>A washing tool (dish soap, sponge, steel wool, scraper, spray) and which dirt types it is effective against.</summary>
    [CreateAssetMenu(menuName = "Zmywak/Tool Definition", fileName = "Tool_")]
    public class ToolDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private List<DirtTypeDefinition> effectiveAgainst = new List<DirtTypeDefinition>();
        [Tooltip("Scrub progress per second when used against a dirt type it is effective against.")]
        [SerializeField] private float cleanPower = 1f;
        [Tooltip("Scrub progress per second when used against the wrong dirt type (still possible, just slow).")]
        [SerializeField] private float ineffectivePower = 0.15f;
        [Tooltip("How wide it works compared with a sponge (1): a spray's jet covers more of the dish at once.")]
        [SerializeField] private float brushRadiusScale = 1f;
        [Tooltip("How thin it can get the dirt, 0-1 of full thickness: a scraper takes the crust off but leaves a film " +
                 "for another tool to finish; 0 cleans right down.")]
        [Range(0f, 1f)] [SerializeField] private float residueFloor;

        public string Id => id;
        public string DisplayName => displayName;
        public float CleanPower => cleanPower;
        public float IneffectivePower => ineffectivePower;
        public float BrushRadiusScale => Mathf.Max(0.1f, brushRadiusScale);
        public float ResidueFloor => residueFloor;

        public bool IsEffectiveAgainst(DirtTypeDefinition dirtType)
        {
            return effectiveAgainst.Contains(dirtType);
        }

        public float PowerAgainst(DirtTypeDefinition dirtType)
        {
            return IsEffectiveAgainst(dirtType) ? cleanPower : ineffectivePower;
        }
    }
}
