using UnityEngine;

namespace Shinobi
{
    [DisallowMultipleComponent]
    public sealed class ShinobiHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool trainingAutoReset = true;
        [SerializeField, Min(0.1f)] private float resetDelay = 2f;
        [SerializeField] private Renderer visual;
        private MaterialPropertyBlock original;
        private MaterialPropertyBlock flash;
        private float flashUntil, resetAt;
        private bool flashing;
        public float CurrentHealth { get; private set; }
        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public bool TrainingAutoReset { get => trainingAutoReset; set => trainingAutoReset = value; }
        public bool Defeated => CurrentHealth <= 0f;
        public event System.Action Damaged;
        public event System.Action<float> DamageApplied;

        private void Awake()
        {
            ResetHealth();
            if (visual == null) visual = GetComponentInChildren<Renderer>();
            if (visual != null)
            {
                original = new MaterialPropertyBlock();
                visual.GetPropertyBlock(original);
                flash = new MaterialPropertyBlock();
                visual.GetPropertyBlock(flash);
                flash.SetColor("_BaseColor", Color.white);
            }
        }
        public void ResetHealth() {
            CurrentHealth = MaxHealth; resetAt = 0f;
            if (flashing && visual != null && original != null) visual.SetPropertyBlock(original);
            flashing = false;
        }
        public bool ApplyDamage(float amount)
        {
            if (Defeated || amount <= 0f) return false;
            float applied = Mathf.Min(CurrentHealth, amount);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Damaged?.Invoke();
            DamageApplied?.Invoke(applied);
            if (visual != null)
            {
                visual.SetPropertyBlock(flash);
                flashing = true;
                flashUntil = Time.time + 0.12f;
            }
            if (Defeated) resetAt = Time.time + Mathf.Max(0.1f, resetDelay);
            return true;
        }
        private void Update()
        {
            if (flashing && Time.time >= flashUntil)
            {
                if (visual != null) visual.SetPropertyBlock(original);
                flashing = false;
            }
            if (Defeated && trainingAutoReset && Time.time >= resetAt) ResetHealth();
        }
        private void OnDisable()
        {
            if (flashing && visual != null) visual.SetPropertyBlock(original);
            flashing = false;
        }
        private void OnGUI()
        {
            if (!ShinobiGameSession.ShowGameplayHud || ShinobiCombatHud.IsActive) return;
            var camera = Camera.main;
            if (camera == null) return;
            var point = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.6f);
            if (point.z <= 0f) return;
            var rect = new Rect(point.x - 60f, Screen.height - point.y, 120f, 24f);
            GUI.Box(rect, $"{CurrentHealth:0} / {MaxHealth:0}");
        }
    }
}

