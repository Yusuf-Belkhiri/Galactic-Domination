using System;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Aircraft
{
    
    /// <summary>
    /// 9 Observations (3 vectors in local space): aircraft velocity, direction to next material, next material forward (orientation), spawn point pos  
    /// 3 Actions: pitchChange, yawChange, boost
    /// </summary>
    public class AircraftAgent : Agent
    {
        //public event Action<Transform> OnCollectMaterial;  
        
    #region FIELDS
    
        [Header("Movement")] 
        [SerializeField] private float _thrust = 1000000f;      // to push the airplane forward (z)
        [SerializeField] private float _pitchSpeed = 100f;      // to rotate vertically (around x)
        [SerializeField] private float _yawSpeed = 100f;       // to rotate around y axis 
        [SerializeField] private float _rollSpeed= 100f;       // to rotate around z axis 
        [SerializeField] private float _boostMultiplier = 2f;   // extra force when the airplane is boosting

        [Header("Explosion")]
        [SerializeField] private GameObject _explosionEffect;
        [SerializeField] private GameObject _meshObject;     // the child mesh object that will disappear on explosion

        [Header("Training")] 
        [Tooltip("Number of steps to time out after in training")] [SerializeField] private int _stepTimeout = 300;     // if the agent does 300 steps (updates), and it hasn't  made it to the next material: reset it (for a better training)
        private float _nextStepTimeOut;
        private bool _frozen;       // whether the aircraft is frozen (intentionally not flying): when paused, or crashed or the at beginning of the race

        // Controls
        private float _pitchChange;         // 0, 1 or -1
        private float _smoothPitchChange;
        [SerializeField] private float _maxPitchAngle = 45f;
        private float _yawChange;       // 0, 1 or -1
        private float _smoothYawChange;
        private float _rollChange;
        private float _smoothRollChange;
        [SerializeField] private float _maxRollAngle = 120f;
        private bool _boost;
        
        // Components
        protected AircraftArea _area;
        private Rigidbody _rb;
        [SerializeField] private TrailRenderer _trail1;
        [SerializeField] private TrailRenderer _trail2;



        public MaterialType RequiredMaterialType { get; private set; } = MaterialType.NoMaterial;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _materialItemHeldPos;
        private bool _isHoldingMaterial;
        public int ExpandProgress { get; private set; }


        [SerializeField] private float _expandRate = 1f;
        #endregion


    #region MLAGENT METHODS

        public override void Initialize()
        {
            _area = GetComponentInParent<AircraftArea>();
            _rb = GetComponent<Rigidbody>();
            //_trail = GetComponent<TrailRenderer>();
            // Override max step: 5000 if training, infinite if racing
            MaxStep = _area._trainingMode ? 5000 : 0;

            ResetRequiredMaterial();
        }

        
        public override void OnEpisodeBegin()
        {
            // Reset the velocity, orientation, trial and position
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _trail1.emitting = false;
            _trail2.emitting = false;
            ResetPosition(); 
            
            // Update the next step timeout
            if (_area._trainingMode)
            {
                _nextStepTimeOut = StepCount + _stepTimeout;
            }
        }
        
        
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.InverseTransformDirection(_rb.velocity));       // aircraft velocity

            if (!_isHoldingMaterial)
            {
                var closestMaterialSpawnPoint = ClosestMaterialSpawnPoint() != null? 
                    ClosestMaterialSpawnPoint() : _area.MaterialSpawnPoints[Random.Range(0, _area.MaterialSpawnPoints.Count)].spawnPoint;
                var closestMaterialDirection = closestMaterialSpawnPoint.position - transform.position;
                sensor.AddObservation(transform.InverseTransformDirection(closestMaterialDirection));        // Where is the next closest material        sensor.AddObservation(NextClosestMaterial());        // Where is the next closest material
                return;
                //Vector3 closestMaterialForward = closestMaterialSpawnPoint.forward;     // orientation
                //sensor.AddObservation(transform.InverseTransformDirection(closestMaterialForward));
            }
            // else
            var spawnPointDirection = _spawnPoint.position - transform.position;
            sensor.AddObservation(transform.InverseTransformDirection(spawnPointDirection));
            
            //Vector3 spawnPointForward = _spawnPoint.forward;     // orientation
            //sensor.AddObservation(transform.InverseTransformDirection(spawnPointForward));
        }

        // Works only if not frozen
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (_frozen) return;

            _pitchChange = actions.DiscreteActions[0];      // 0: don't move,   1: move up,     2: move down (-1)
            if (_pitchChange == 2) _pitchChange = -1f;

            _yawChange = actions.DiscreteActions[1];        // 0: don't move,   1: move right,  2: move left (-1)
            if (_yawChange == 2) _yawChange = -2;
            
            _boost = actions.DiscreteActions[2] == 1;
            if (_boost && !_trail1.emitting)
            {
                _trail1.Clear();
                _trail2.Clear();
            }
                
            _trail1.emitting = _boost;
            _trail2.emitting = _boost;
            
            ProcessMovement();
            
            if (_area._trainingMode)
            {
                // Small negative reward every step to accelerate training
                AddReward(-1f / MaxStep);

                if (StepCount > _nextStepTimeOut)
                {
                    var materialItemHeld = _materialItemHeldPos.GetComponentInChildren<MaterialItem>();
                    if (materialItemHeld != null )
                    {
                        Destroy(materialItemHeld.gameObject);
                    }
                    _isHoldingMaterial = false;
                    
                    AddReward(-0.5f); 
                    EndEpisode();
                }

                
                // CURRICULUM LEARNING
                
                // COLLECT ITEM ASSISTANCE
                if (!_isHoldingMaterial)
                {
                    var closestMatSpawnPoint = ClosestMaterialSpawnPoint();
                    if (closestMatSpawnPoint == null)
                    {
                        return;
                    }
                    Vector3 localMaterialDir = transform.InverseTransformDirection(closestMatSpawnPoint.position - transform.position);
                    if (localMaterialDir.magnitude >
                          Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f)) return;
                    // Assistance
                    print("Smaller distance than: " + Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f));
                    GotMaterial(closestMatSpawnPoint);
                    var hh = _area.MaterialSpawnPoints.Find(m => m.materialItem == _materialItemHeldPos.GetComponentInChildren<MaterialItem>());
                    if (hh != null)
                    {
                        hh.materialItem = null;
                    }
                    return;
                }
                // DELIVERY ASSISTANCE
                Vector3 localSpawnPointDir = transform.InverseTransformDirection(_spawnPoint.position - transform.position);
                // Assistance
                if (localSpawnPointDir.magnitude < Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f))
                {
                    DeliveredMaterial();
                }
            }
            
        }

        // Prevent using Heuristic behaviour in AircraftAgent
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            Debug.LogError($"Heuristic() was called on {gameObject.name}. " +
                           "Make sure only the AircraftPlayer is set to Behaviour Type: Heuristic Only.");
        }

        #endregion

        
        private Transform ClosestMaterialSpawnPoint()
        {
            var nextMaterialList = _area.MaterialSpawnPoints.FindAll(m => m.materialItem != null && m.materialItem.MaterialType == RequiredMaterialType);
            if (nextMaterialList.Count == 0) return null;
            
            nextMaterialList.Sort(MaterialPosComparer);
            //return nextMaterialList[0].spawnPoint;
            return nextMaterialList[0].materialItem.transform;
        }

        private int MaterialPosComparer(AircraftArea.MaterialSpawnPoint a, AircraftArea.MaterialSpawnPoint b)
        {
            var distanceToA = Vector3.Distance(transform.position, a.materialItem.transform.position);      // it was a.spawnPoint.position
            var distanceToB = Vector3.Distance(transform.position, b.materialItem.transform.position);
            return distanceToA.CompareTo(distanceToB);
        }
        
        
        private void GotMaterial(Transform materialItem)
        {
            _isHoldingMaterial = true;
            
            //_area.RemoveMaterial(ClosestMaterialSpawnPoint());
            
            //ClosestMaterialSpawnPoint().gameObject.SetActive(false);

            materialItem.GetComponent<Collider>().enabled = false;
            
            materialItem.position = _materialItemHeldPos.position;
            materialItem.SetParent(_materialItemHeldPos);
            
            if (_area._trainingMode)
            {
                AddReward(1f);      // 0.5f
                _nextStepTimeOut = StepCount + _stepTimeout;
            }
            
            
            //materialItem.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
            
            AudioManager.Instance.Play(AudioClipsNames.CollectItem1, false);
        }

        private  void DeliveredMaterial()
        {
            var materialItem = _materialItemHeldPos.GetComponentInChildren<MaterialItem>();
            if (materialItem == null)
            {
                return;     // if so, there is a problem (something related to _isHoldingMaterial is missed)
            }
            
            _isHoldingMaterial = false;
            ExpandProgress += 10;
            print(ExpandProgress + gameObject.name);
            ResetRequiredMaterial();        // Generate random nextMaterial

            //materialItem.transform.position = _spawnPoint.position;
            //materialItem.transform.SetParent(_spawnPoint);
            Destroy(materialItem.gameObject);
            
            // LATER
            _spawnPoint.localScale += _expandRate * Vector3.one;
            _spawnPoint.localPosition += new Vector3(0, 1);
            
            
            if (_area._trainingMode)
            {
                AddReward(1f);      // 0.5f
                _nextStepTimeOut = StepCount + _stepTimeout;
            }
            
            AudioManager.Instance.Play(AudioClipsNames.CollectItem1, false);
        }

        // Prevent the agent from moving and taking actions
        public void FreezeAgent()
        {
            Debug.Assert(!_area._trainingMode, "Freeze/Thaw is not supported in training");
            _frozen = true;
            _rb.Sleep();
            _trail1.emitting = false;
            _trail2.emitting = false;
        }

        // != FreezeAgent, Resume the agent movement and actions 
        public void ThawAgent()
        {
            Debug.Assert(!_area._trainingMode, "Freeze/Thraw is not supported in training");
            _frozen = false;
            _rb.WakeUp();
        }
        

        // Calculate & apply movement
        private void ProcessMovement()
        {
            // Move forward
            var boostModifier = _boost ? _boostMultiplier : 1f;
            _rb.AddForce(transform.forward * _thrust * boostModifier, ForceMode.Force); 
            
            // Rotations 
            Vector3 currentRot = transform.rotation.eulerAngles;        // to use clamp below (the use of:transform.rotation = instead of transform.Rotate()) 
            
            if (_yawChange == 0f)
            {
                float rollAngle = currentRot.z > 180f ? currentRot.z - 360 : currentRot.z;       // between (-180, 180)
                _rollChange = -rollAngle / _maxRollAngle;
            }
            else
                _rollChange = -_yawChange;      // the opposite direction

                // Smooth rotations 
            _smoothPitchChange = Mathf.MoveTowards(_smoothPitchChange, _pitchChange, 2f * Time.fixedDeltaTime);
            _smoothYawChange = Mathf.MoveTowards(_smoothYawChange, _yawChange, 2f * Time.deltaTime);
            _smoothRollChange = Mathf.MoveTowards(_smoothRollChange, _rollChange, 2f * Time.deltaTime);

            float pitch = currentRot.x + _pitchSpeed * _smoothPitchChange * Time.fixedDeltaTime;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -_maxPitchAngle, _maxPitchAngle);

            float yaw = currentRot.y + _yawSpeed * _smoothYawChange * Time.fixedDeltaTime;

            float roll = currentRot.x + _rollSpeed * _smoothRollChange * Time.fixedDeltaTime;
            if (roll > 180f) roll -= 360f;
            roll = Mathf.Clamp(roll, -_maxRollAngle, _maxRollAngle);
            
            
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }


        private void OnTriggerEnter(Collider other)
        {
//            print(other.GetComponentInChildren<MaterialItem>().MaterialName);
            switch (_isHoldingMaterial)
            {
                // Collect material
                // case false when other.tag == "MaterialSpawnPoint" && other.GetComponentInChildren<MaterialItem>().MaterialType == _requiredMaterialType:
                //     print($"Collected {other.GetComponentInChildren<MaterialItem>().MaterialType}");
                //
                //     var hh = _area.MaterialSpawnPoints.Find(m => m.spawnPoint == other.transform);
                //     hh._MaterialItemName = MaterialType.NoMaterial;
                //     
                //     GotMaterial(other.GetComponentInChildren<MaterialItem>().gameObject);
                //     break;
                // case true when other.transform == _spawnPoint:          // Deliver material
                //     DeliveredMaterial();
                //     break;
                case false when other.CompareTag("MaterialItem") && other.TryGetComponent(out MaterialItem materialItem) && materialItem.MaterialType == RequiredMaterialType:
                    //print($"Collected {other.GetComponentInChildren<MaterialItem>().MaterialType}");

                    var hh = _area.MaterialSpawnPoints.Find(m => m.materialItem == materialItem);
                    hh.materialItem = null;

                    //other.GetComponent<Collider>().enabled = false;
                    
                    GotMaterial(materialItem.transform);
                    break;
                case true when other.CompareTag("AircraftSpawnPoint") && other.transform == _spawnPoint:          // Deliver material
                    DeliveredMaterial();
                    break;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Agent"))
            {
                var materialItemHeld = _materialItemHeldPos.GetComponentInChildren<MaterialItem>();
                if (materialItemHeld != null )
                {
                    Destroy(materialItemHeld.gameObject);
                }
                _isHoldingMaterial = false;
                
                
                if (_area._trainingMode)
                {
                    AddReward(-1f);
                    EndEpisode();
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }


        // Resets the aircraft to the most recent completed checkpoint
        private IEnumerator ExplosionReset()
        {
            FreezeAgent();

            AudioManager.Instance.Play(AudioClipsNames.Explosion, false);
            
            _meshObject.SetActive(false);
            _explosionEffect.SetActive(true);
            
            yield return new WaitForSeconds(2f);    
            _meshObject.SetActive(true);
            _explosionEffect.SetActive(false);
            ResetPosition();//transform.position = _spawnPoint.position; //_area.ResetAgentPosition(agent:this);

            yield return new WaitForSeconds(1f);
            
            ThawAgent();
        }


        private void ResetPosition()
        {
            transform.position = _spawnPoint.position;
            transform.rotation = _spawnPoint.rotation;
        }


        private void ResetRequiredMaterial()
        {
            RequiredMaterialType = (MaterialType)Random.Range(1, Enum.GetNames(typeof(MaterialType)).Length);
            
            // if(thisOne)
            //     print(_requiredMaterialType);
        }


        //public bool thisOne;


        // private void FixedUpdate()
        // {
        //     if (thisOne)
        //     {
        //         print(_isHoldingMaterial);
        //     }
        // }
    }
}
