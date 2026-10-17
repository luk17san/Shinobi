using UnityEngine;
using UnityEngine.InputSystem;

namespace Shinobi
{
    // Temporary arena feedback; the combat profile remains the source of controls and costs.
    public sealed class ShinobiTrainingHud : MonoBehaviour
    {
        [SerializeField] private ShinobiMovement player;
        [SerializeField] private Vector2 position = new Vector2(12, 12);
        [SerializeField, Min(300)] private float width = 460;
        [SerializeField] private bool showControls;
        private ShinobiCombat combat;
        private ShinobiTechniqueController techniques;
        private ShinobiHealth health;
        private ShinobiWeaponInventory weapons;
        private ShinobiDefense defense;
        private ShinobiTrainingOpponent opponent;

        private void Awake()
        {
            if (player == null) return;
            combat = player.GetComponent<ShinobiCombat>();
            techniques = player.GetComponent<ShinobiTechniqueController>();
            health = player.GetComponent<ShinobiHealth>();
            weapons = player.GetComponent<ShinobiWeaponInventory>();
            defense = player.GetComponent<ShinobiDefense>();
            opponent = FindFirstObjectByType<ShinobiTrainingOpponent>();
        }

        private void OnGUI()
        {
            if (!ShinobiGameSession.ShowGameplayHud) return;
            if (player == null || combat == null || combat.profile == null || techniques == null) return;
            var profile = combat.profile;
            GUILayout.BeginArea(new Rect(position.x, position.y, Mathf.Min(width, Screen.width - position.x * 2), showControls ? (ShinobiGameSession.DuelActive ? 360 : 485) : (ShinobiGameSession.DuelActive ? 165 : 300)), GUI.skin.box);
            GUILayout.Label("TRENING WALKI I JUTSU");
            GUILayout.Label($"HP: {(health != null ? health.CurrentHealth : 0):0}   Chakra: {techniques.Chakra:0}/{profile.maxChakra:0}");
            if (defense != null) GUILayout.Label($"Wytrzymalosc: {defense.Stamina:0}/{defense.MaxStamina:0} | "
                + (defense.GuardBroken ? "Przelamanie gardy" : defense.BlockedRecently ? "Blok" : profile.autoBlock ? "Autoblok" : "Blok wylaczony"));
            if (weapons != null) GUILayout.Label($"Bron: {weapons.CurrentWeaponName} | {KeyName(profile.nextWeaponKey)}: nastepna");
            GUILayout.Label("2x A/D: dash | " + (player.IsDashing ? "Dash" : player.DashCooldownRemaining > 0 ? $"Gotowy za {player.DashCooldownRemaining:0.0}s" : "Gotowy"));
            showControls = GUILayout.Toggle(showControls, "Sterowanie i stan");
            if (showControls)
            {
            GUILayout.Label($"A/D: ruch | {KeyName(profile.jumpKey)}: skok | {KeyName(profile.crouchKey)}: kucanie");
            GUILayout.Label($"{KeyName(profile.meleeKey)}: cios | {KeyName(profile.throwKey)}: rzut | " + (ShinobiGameSession.DuelActive ? "Esc: pauza" : "R: reset treningu"));
            GUILayout.Label($"Przytrzymaj {KeyName(profile.focusKey)}, wpisz pieczecie, pusc przycisk.");
            if (profile.jutsu != null && profile.jutsu.Length > 0 && profile.jutsu[0] != null)
            {
                var jutsu = profile.jutsu[0];
                GUILayout.Label($"{jutsu.name}: {Seals(jutsu.sequence == null ? "" : string.Join(", ", jutsu.sequence))} | koszt {jutsu.chakraCost:0}");
            }
            GUILayout.Label("Pieczecie: " + (techniques.Sequence.Length > 0 ? Seals(techniques.Sequence) : "—"));
            GUILayout.Label(techniques.Focus ? "Skladanie pieczeci..." : techniques.Busy ? "Wykonywanie jutsu..." : "Gotowy");
            GUILayout.Label(player.ActiveState);
            }
            if (defense != null && !ShinobiGameSession.DuelActive)
            {
                GUILayout.Label("Test obrony (25 obrazen):");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Cios z przodu")) defense.ReceiveHit(25, player.transform.position + Vector3.right * player.Facing, true);
                if (GUILayout.Button("Cios z tylu")) defense.ReceiveHit(25, player.transform.position - Vector3.right * player.Facing, true);
                if (GUILayout.Button("Jutsu")) defense.ReceiveHit(25, player.transform.position + Vector3.right * player.Facing, false);
                GUILayout.EndHorizontal();
            }
            if (opponent != null && !ShinobiGameSession.DuelActive)
            {
                GUILayout.Label("Rywal: " + opponent.Status);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(opponent.Running ? "Pauza sparingu" : "Rozpocznij sparing")) opponent.SetRunning(!opponent.Running);
                if (GUILayout.Button("Reset sparingu")) { opponent.ResetTraining(); health.ResetHealth(); if (defense != null) defense.ResetDefense(); }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private static string Seals(string value) => value.Replace("Up", "W").Replace("Down", "S").Replace("Left", "A").Replace("Right", "D");
        private static string KeyName(Key key) => key == Key.UpArrow ? "Gora" : key == Key.Space ? "Spacja" : key == Key.LeftShift ? "LShift" : key.ToString();
    }
}


