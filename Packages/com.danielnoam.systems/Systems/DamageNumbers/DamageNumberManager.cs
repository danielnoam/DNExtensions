using DNExtensions.Systems.ObjectPooling;
using UnityEngine;

namespace DNExtensions.Systems.DamageNumbers
{
    public readonly struct DamageNumberInfo
    {
        public readonly float Amount;
        public readonly Vector3 Position;
        public readonly bool IsCritical;
        public readonly string Text;

        public DamageNumberInfo(float amount, Vector3 position, bool isCritical = false, string text = null)
        {
            Amount = amount;
            Position = position;
            IsCritical = isCritical;
            Text = text;
        }
    }
    
    [AddComponentMenu("DNExtensions/Damage Number Manager")]
    public class DamageNumberManager : MonoBehaviour
    {
        public static DamageNumberManager Instance { get; private set; }

        [SerializeField] private DamageNumberPopup popupPrefab;

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(DamageNumberInfo info)
        {
            if (!popupPrefab) return;

            DamageNumberPopup popup = ObjectPooler.GetObjectFromPool(popupPrefab, info.Position);
            if (!popup) return;

            popup.transform.SetParent(transform, true);
            popup.Show(info);
        }
    }
}
