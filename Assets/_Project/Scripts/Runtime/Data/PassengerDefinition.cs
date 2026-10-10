using System.Collections.Generic;
using UnityEngine;

namespace LastFare.Data
{
    /// <summary>
    /// Static description of a passenger type. Behaviour lives in code/Timeline;
    /// this asset only holds the data designers tune. See docs/passengers/.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Fare/Passenger", fileName = "Passenger_")]
    public class PassengerDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Unknown";
        [Range(1f, 5f)] [SerializeField] float shownRating = 4.8f;

        [Tooltip("Clues visible on the ride request screen before accepting.")]
        [SerializeField] List<string> redFlags = new();

        [Tooltip("Survival rules the player can discover during the ride (shown in the journal once learned).")]
        [SerializeField] List<string> rules = new();

        [SerializeField] GameObject prefab;

        /// <summary>Builds an unsaved instance in code (fallbacks, tests, and the editor setup tool).</summary>
        public static PassengerDefinition CreateRuntime(string name, float rating, IEnumerable<string> flags, IEnumerable<string> passengerRules)
        {
            var p = CreateInstance<PassengerDefinition>();
            p.displayName = name;
            p.shownRating = rating;
            p.redFlags = new List<string>(flags);
            p.rules = new List<string>(passengerRules);
            return p;
        }

        public string DisplayName => displayName;
        public float ShownRating => shownRating;
        public IReadOnlyList<string> RedFlags => redFlags;
        public IReadOnlyList<string> Rules => rules;
        public GameObject Prefab => prefab;
    }
}
