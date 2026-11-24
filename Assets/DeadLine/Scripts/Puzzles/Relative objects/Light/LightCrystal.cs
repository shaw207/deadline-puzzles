using UnityEngine;

namespace DeadLine
{
    public class LightCrystal : MonoBehaviour
    {
        [SerializeField] private int symbolId;
        public int SymbolId => symbolId;

        public Rigidbody RB { get; private set; }
        public Collider Col { get; private set; }

        private void Awake()
        {
            RB = GetComponent<Rigidbody>();
            Col = GetComponent<Collider>();
        }
    }
}
