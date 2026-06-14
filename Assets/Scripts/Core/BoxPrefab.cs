using UnityEngine;

namespace Core
{
    /// <summary>
    /// Component attached to conveyor belt box prefabs.
    /// Defines the color group and provides easy identification for the box spawning system.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BoxPrefab : MonoBehaviour
    {
        [SerializeField] private ColorGroup _colorGroup;
        
        private SpriteRenderer _spriteRenderer;

        public ColorGroup ColorGroup => _colorGroup;
        public SpriteRenderer SpriteRenderer
        {
            get
            {
                if (_spriteRenderer == null)
                    _spriteRenderer = GetComponent<SpriteRenderer>();
                return _spriteRenderer;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (SpriteRenderer != null)
                _colorGroup.ApplyTo(SpriteRenderer);
        }
#endif
    }
}