using UnityEngine;

namespace DeadLine
{
    public class WaterSurfaceFollow : MonoBehaviour
    {
        [SerializeField] private WaterLevelController water;

        private void Update()
        {
            var p = transform.position;
            p.y = water.CurrentSurfaceY;
            transform.position = p;
        }
    }
}