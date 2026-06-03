using UnityEngine;
using UnityEngine.Events;

namespace ArrowGame.Data.VFX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class CoinParticleAttractor : MonoBehaviour
    {
        [Header("--- Curve Flight Settings ---")]
        [SerializeField] private float delayBeforeAttract = 0.4f; 
        [SerializeField] private float steerSpeed = 15f; 
        [SerializeField] private float acceleration = 3000f; 
        [SerializeField] private float destroyDistance = 1f; 

        [Header("--- Events ---")]
        public UnityEvent onCoinReachedTarget;
        public UnityEvent onAllCoinsReached;

        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private Transform _target;
        
        private bool _isActive = false;
        private float _timer = 0f;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
        }

        public void PlayCoinFlight(Transform target)
        {
            _target = target;
            _timer = 0f;
            _isActive = true;
            _particleSystem.Play();
        }

        private void LateUpdate()
        {
            if (!_isActive || _target == null) return;

            _timer += Time.deltaTime;
            int aliveParticles = _particleSystem.GetParticles(_particles);

            if (aliveParticles == 0 && _timer > delayBeforeAttract)
            {
                _isActive = false;
                onAllCoinsReached?.Invoke();
                return;
            }

            for (int i = 0; i < aliveParticles; i++)
            {
                Vector3 targetPos = _target.position;
                Vector3 currentPos = _particles[i].position;
                Vector3 currentVel = _particles[i].velocity;

                float distance = Vector3.Distance(currentPos, targetPos);
                
                if (_timer >= delayBeforeAttract)
                {
                    if (distance < destroyDistance)
                    {
                        _particles[i].remainingLifetime = 0f;
                        onCoinReachedTarget?.Invoke();
                    }
                    else
                    {
                        Vector3 desiredDirection = (targetPos - currentPos).normalized;
                        Vector3 newDirection = Vector3.RotateTowards(
                            currentVel.normalized, 
                            desiredDirection, 
                            steerSpeed * Time.deltaTime, 
                            0f
                        );

                        float newSpeed = currentVel.magnitude + (acceleration * Time.deltaTime);
                        _particles[i].velocity = newDirection * newSpeed;
                    }
                }
            }

            _particleSystem.SetParticles(_particles, aliveParticles);
        }
    }
}