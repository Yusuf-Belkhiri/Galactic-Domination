using UnityEngine;
using Random = UnityEngine.Random;

namespace Aircraft
{
    public class Rotate : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotationSpeed;
        [SerializeField] private bool _randomize;       // Whether to randomize the start rotation

        private void Start()
        {
            if (_randomize)
                transform.Rotate(_rotationSpeed.normalized * Random.Range(0, 390));     // pick a random start rotation
        }

        private void Update()
        {
            transform.Rotate(_rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
