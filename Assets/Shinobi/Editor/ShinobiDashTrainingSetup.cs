using UnityEditor;
using UnityEngine;

namespace Shinobi.Editor
{
    public static class ShinobiDashTrainingSetup
    {
        [MenuItem("Shinobi/Etap 7/Przygotuj szybki dash")]
        public static void Apply()
        {
            ShinobiDefenseTrainingSetup.Apply();
            Debug.Log("DASH_SETUP_PASS: zapisano ustawienia dasha w TrainingArena, prefabie i profilu sterowania.");
        }
    }
}
