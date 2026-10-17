using System.Collections.Generic;
using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent, RequireComponent(typeof(ShinobiCombat))]
    public sealed class ShinobiWeaponInventory : MonoBehaviour
    {
        [SerializeField] private List<ShinobiWeaponDefinition> weapons = new List<ShinobiWeaponDefinition>();
        [SerializeField, Min(0)] private int initialWeaponIndex;
        [SerializeField] private Transform weaponSocket;
        private int selectedIndex = -1;
        private ShinobiCombat combat;
        private ShinobiTechniqueController techniques;
        private ShinobiHealth health;
        private ShinobiDefense defense;
        private ShinobiMovement movement;
        private GameObject currentVisual;
        private bool thrownVisualHidden;
        public Transform WeaponSocket => weaponSocket;
        public void SetThrownVisualHidden(bool hidden)
        {
            thrownVisualHidden = hidden;
            if (currentVisual != null) currentVisual.SetActive(!hidden);
        }

        public IReadOnlyList<ShinobiWeaponDefinition> Weapons => weapons;
        public int SelectedIndex => selectedIndex;
        public ShinobiWeaponDefinition CurrentWeapon => weapons != null && selectedIndex >= 0 && selectedIndex < weapons.Count
            && weapons[selectedIndex] != null && weapons[selectedIndex].IsUsable ? weapons[selectedIndex] : null;
        public string CurrentWeaponName => CurrentWeapon != null ? CurrentWeapon.displayName : "Walka wrecz";

        private void Awake()
        {
            combat = GetComponent<ShinobiCombat>();
            techniques = GetComponent<ShinobiTechniqueController>();
            health = GetComponent<ShinobiHealth>();
            defense = GetComponent<ShinobiDefense>();
            movement = GetComponent<ShinobiMovement>();
            if (weapons == null || weapons.Count == 0) return;
            int first = Mathf.Clamp(initialWeaponIndex, 0, weapons.Count - 1);
            for (int offset = 0; offset < weapons.Count; offset++)
            {
                int index = (first + offset) % weapons.Count;
                if (weapons[index] != null && weapons[index].IsUsable) { selectedIndex = index; break; }
            }
        }
        private void OnEnable() { RefreshVisual(); }
        private void OnDisable() { ClearVisual(); }
        private bool CanSwitch => isActiveAndEnabled && combat != null && !combat.IsAttacking
            && (GetComponent<ShinobiThrowController>() == null || !GetComponent<ShinobiThrowController>().IsThrowing)
            && ShinobiGameSession.GameplayInputAllowed
            && (techniques == null || !techniques.Busy) && (health == null || !health.Defeated) && (defense == null || !defense.GuardBroken)
            && (movement == null || !movement.IsDashing);

        public bool TryEquipNext()
        {
            if (!CanSwitch || weapons == null || weapons.Count == 0) return false;
            int start = selectedIndex >= 0 && selectedIndex < weapons.Count ? selectedIndex : -1;
            for (int offset = 1; offset <= weapons.Count; offset++)
            {
                int next = (start + offset) % weapons.Count;
                if (next != selectedIndex && weapons[next] != null && weapons[next].IsUsable) return TryEquip(next);
            }
            return false;
        }
        public void ResetEquipment()
        {
            selectedIndex = -1;
            if (weapons != null && weapons.Count > 0)
            {
                int first = Mathf.Clamp(initialWeaponIndex, 0, weapons.Count - 1);
                for (int offset = 0; offset < weapons.Count; offset++)
                {
                    int index = (first + offset) % weapons.Count;
                    if (weapons[index] != null && weapons[index].IsUsable) { selectedIndex = index; break; }
                }
            }
            RefreshVisual();
        }
        public bool TryEquip(int index)
        {
            if (!CanSwitch || weapons == null || index < 0 || index >= weapons.Count || index == selectedIndex
                || weapons[index] == null || !weapons[index].IsUsable) return false;
            selectedIndex = index;
            RefreshVisual();
            return true;
        }
        private void ClearVisual()
        {
            if (currentVisual == null) return;
            currentVisual.SetActive(false);
            Destroy(currentVisual);
            currentVisual = null;
        }
        private void RefreshVisual()
        {
            ClearVisual();
            var weapon = CurrentWeapon;
            if (weapon == null || weapon.visualPrefab == null || weaponSocket == null) return;
            currentVisual = Instantiate(weapon.visualPrefab, weaponSocket, false);
            currentVisual.transform.localPosition = weapon.localPosition;
            currentVisual.transform.localRotation = Quaternion.Euler(weapon.localEulerAngles);
            currentVisual.transform.localScale = weapon.localScale;
            currentVisual.SetActive(!thrownVisualHidden);
            // Held models are visuals; hit detection belongs to ShinobiCombat.
            foreach (var collider in currentVisual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in currentVisual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        }
    }
}
