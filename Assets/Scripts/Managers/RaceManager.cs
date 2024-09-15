using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class RaceManager : MonoBehaviour
    {
        // Pairs of: Difficulty, NNModel (to be serializable & visible in the inspector, instead of dictionary)
        [Serializable]
        public struct DifficultyModel
        {
            public GameDifficulty _difficulty;
            public NNModel _model;
        }
        
        private class AircraftStatus
        {
            public int _checkpointIndex = 0;
            public int _lap = 0;
            public int _place = 0;
            public float _timeRemaining = 0;
        }
        
        #region FIELDS

        // Agents 
        [SerializeField] private AircraftArea _aircraftArea;
        private AircraftPlayer _aircraftPlayer;
        private List<AircraftAgent> _sortedAircraftAgents;
        private Dictionary<AircraftAgent, AircraftStatus> _aircraftStatus;
        public AircraftAgent FollowedAgent { get; private set; }        // the agent being followed by the camera

        public Camera ActiveCamera { get; private set; }
        [SerializeField] private CinemachineVirtualCamera _virtualCamera;
        
        [SerializeField] private int _numLaps = 2;
        public int NumLaps => _numLaps;
        [SerializeField] private List<DifficultyModel> _difficultyModels;
        
        
        // UI
        [SerializeField] private CountdownUIController _countdownUI;
        [SerializeField] private PauseMenuController _pauseMenu;
        [SerializeField] private HUDController _hudController;
        [SerializeField] private GameOverUIController _gameOverUI;

        
        // Time & Pause
        private float _lastResumeTime;
        private float _previouslyElapsedTime;
        [SerializeField] private float checkpointBonusTime = 15;

        [SerializeField] private float _orderPlaceUpdateCooldown = 0.5f;
        private float _lastPlaceUpdate;     // last time the order of agents was checked

        public float RaceTime       // the clock keeping track of race time (considering pauses)
        {
            get
            {
                return GameManager.Instance.GameState switch
                {
                    GameState.Playing => _previouslyElapsedTime + Time.time - _lastResumeTime,
                    GameState.Paused => _previouslyElapsedTime,
                    _ => 0
                };
            }
        }

        #endregion


        #region METHODS

        // private void Awake()
        // {
        //     // hud (UiManager), countdownUI, pauseMenu, gameOverUI, virtualCamera, aircraftArea, ActiveCamera
        // }

        private void Start()
        {
            _sortedAircraftAgents = new List<AircraftAgent>(_aircraftArea.AircraftAgents);
            
            ActiveCamera = Camera.main;

            GameManager.Instance.OnStateChange += OnStateChange;

            FollowedAgent = _aircraftArea.AircraftAgents[0];

            foreach (var agent in _aircraftArea.AircraftAgents)
            {
                agent.FreezeAgent();
                if (agent.GetType() == typeof(AircraftPlayer))
                {
                    FollowedAgent = agent;
                    _aircraftPlayer = (AircraftPlayer)agent;
                    _aircraftPlayer._pauseInput.performed += PauseInputPerformed;
                }
                else
                {
                    agent.SetModel(GameManager.Instance.GameDifficulty.ToString(), 
                        _difficultyModels.Find(x => x._difficulty == GameManager.Instance.GameDifficulty)._model);
                }
            }

            _virtualCamera.Follow = FollowedAgent.transform;
            _virtualCamera.LookAt = FollowedAgent.transform;
            _hudController.FollowedAgent = FollowedAgent;
            
            // Hide UI
            _hudController.gameObject.SetActive(false);
            _pauseMenu.gameObject.SetActive(false);
            _countdownUI.gameObject.SetActive(false);
            _gameOverUI.gameObject.SetActive(false);

            StartCoroutine(StartRace());
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {   
                // Update the order list every _orderPlaceUpdateTime
                if (_lastPlaceUpdate + _orderPlaceUpdateCooldown < Time.fixedTime)
                {
                    _lastPlaceUpdate = Time.fixedTime;
                    
                    _sortedAircraftAgents.Sort((a, b) => PlaceComparer(a, b));

                    for (int i = 0; i < _sortedAircraftAgents.Count; i++)
                    {
                        _aircraftStatus[_sortedAircraftAgents[i]]._place = i + 1;
                    }
                }
                // Update status
                /*foreach (AircraftAgent agent in _aircraftArea.AircraftAgents)
                {
                    AircraftStatus status = _aircraftStatus[agent];
                    if (status._checkpointIndex != agent.NextCheckpointIndex)       // Checkpoint
                    {
                        status._checkpointIndex = agent.NextCheckpointIndex;
                        status._timeRemaining = checkpointBonusTime;
                        
                        if (status._checkpointIndex  == 0)          // Laps
                        {
                            status._lap++;
                            if (FollowedAgent == agent && status._lap > _numLaps)       
                            {
                                GameManager.Instance.GameState = GameState.GameOver;
                            }
                        }
                    }

                    status._timeRemaining = Mathf.Max(0, status._timeRemaining - Time.fixedDeltaTime);
                    if (status._timeRemaining == 0)
                    {
                        agent.ResetPosition();//_aircraftArea.ResetAgentPosition(agent);
                        status._timeRemaining = checkpointBonusTime;
                    }
                }*/
            }
        }

        // -1: a is before b
        private int PlaceComparer(AircraftAgent a, AircraftAgent b)
        {
            AircraftStatus statusA = _aircraftStatus[a];
            AircraftStatus statusB = _aircraftStatus[b];

            int checkpointA = statusA._checkpointIndex + (statusA._lap - 1) * _aircraftArea.MaterialSpawnPoints.Count;
            int checkpointB = statusB._checkpointIndex + (statusB._lap - 1) * _aircraftArea.MaterialSpawnPoints.Count;

            int compare = 0;
            if (checkpointA == checkpointB)
            {
                Vector3 nextCheckpointPos = GetAgentCheckpoint(a).position;
                compare = Vector3.Distance(a.transform.position, nextCheckpointPos)
                    .CompareTo(Vector3.Distance(b.transform.position, nextCheckpointPos));
            }
            else
            {
                compare = -1 * checkpointA.CompareTo(checkpointB);
            }
            return compare;
        }

        // Can only be performed when Playing
        private void PauseInputPerformed(InputAction.CallbackContext obj)
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                GameManager.Instance.GameState = GameState.Paused;
            }
        }

        private void OnStateChange(GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                {
                    // Start/Resume game time, show UI, thaw the agents
                    _lastResumeTime = Time.time;
                    _hudController.gameObject.SetActive(true);
                    foreach (var agent in _aircraftArea.AircraftAgents) agent.ThawAgent();
                    break;
                }
                case GameState.Paused:
                {
                    // Pause game time, freeze the agents
                    _previouslyElapsedTime = Time.time - _lastResumeTime;
                    foreach (var agent in _aircraftArea.AircraftAgents) agent.FreezeAgent();
                    
                    _pauseMenu.gameObject.SetActive(true);
                    break;
                }
                case GameState.GameOver:
                {
                    // Pause game time, hide UI & show GameOverUI, freeze the agents
                    _previouslyElapsedTime = Time.time - _lastResumeTime;
                    foreach (var agent in _aircraftArea.AircraftAgents) agent.FreezeAgent();
                    _hudController.gameObject.SetActive(false);
                    _gameOverUI.gameObject.SetActive(true);
                    break;
                }
                default:
                    _lastResumeTime = 0;
                    _previouslyElapsedTime = 0;
                    break;
            }
        }

        private IEnumerator StartRace()
        {
            // Countdown
            _countdownUI.gameObject.SetActive(true);
            yield return _countdownUI.StartCountdown();

            // Initialize agents & Start
            _aircraftStatus = new Dictionary<AircraftAgent, AircraftStatus>();
            foreach (var agent in _aircraftArea.AircraftAgents)
            {
                AircraftStatus status = new AircraftStatus
                {
                    _lap = 1,
                    _timeRemaining = checkpointBonusTime
                };
                _aircraftStatus.Add(agent, status);
            }
            GameManager.Instance.GameState = GameState.Playing;
        }


        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChange -= OnStateChange;
            }

            if (_aircraftPlayer != null)
            {
                _aircraftPlayer._pauseInput.performed -= PauseInputPerformed;
            }
        }

        
        // used by UI
        public Transform GetAgentCheckpoint(AircraftAgent agent)
        {
            //return _aircraftArea.Checkpoints[agent.NextCheckpointIndex];          Ig it can be used instead
            return _aircraftArea.MaterialSpawnPoints[_aircraftStatus[agent]._checkpointIndex].spawnPoint;
        }
        
        
        public int GetAgentLap(AircraftAgent agent)
        {
            return _aircraftStatus[agent]._lap;
        }

        public string GetAgentPlace(AircraftAgent agent)
        {
            int place = _aircraftStatus[agent]._place;

            return place switch
            {
                <= 0 => string.Empty,
                >= 11 and <= 13 => $"{place}th",
                _ => (place % 10) switch
                {
                    1 => $"{place}st",
                    2 => $"{place}nd",
                    3 => $"{place}rd",
                    _ => $"{place}th"
                }
            };
        }

        public float GetAgentTime(AircraftAgent agent)
        {
            return _aircraftStatus[agent]._timeRemaining;
        }
        #endregion
        
    }
}
