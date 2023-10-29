using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class GameplayManager : MonoBehaviour
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
            public MaterialType _requiredMaterial = MaterialType.NoMaterial;
            public int _expandProgress = 0;
            public int _place = 0;
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
        //[SerializeField] private List<DifficultyModel> _difficultyModels;
        
        
        // UI
        [SerializeField] private CountdownUIController _countdownUI;
        [SerializeField] private PauseMenuController _pauseMenu;
        [SerializeField] private HUDController _hudController;
        [SerializeField] private GameOverUIController _gameOverUI;



        [SerializeField] private float _orderPlaceUpdateCooldown = 0.5f;
        private float _lastPlaceUpdate;     // last time the order of agents was checked
        

        #endregion
        
        
        private void Start()
        {
            _sortedAircraftAgents = new List<AircraftAgent>(_aircraftArea.AircraftAgents);

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
                // else
                // {
                //     agent.SetModel(GameManager.Instance.GameDifficulty.ToString(), 
                //         _difficultyModels.Find(x => x._difficulty == GameManager.Instance.GameDifficulty)._model);
                // }
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
                    
                    _sortedAircraftAgents.Sort(PlaceComparer);

                    for (int i = 0; i < _sortedAircraftAgents.Count; i++)
                    {
                        _aircraftStatus[_sortedAircraftAgents[i]]._place = i + 1;
                    }
                }
                // Update status
                /**/
                foreach (AircraftAgent agent in _aircraftArea.AircraftAgents)
                {
                    AircraftStatus status = _aircraftStatus[agent];
                    status._requiredMaterial = agent.RequiredMaterialType;
                    status._expandProgress = agent.ExpandProgress;
                    if (status._expandProgress >= 100)
                    {
                        // CHECK IF PLAYER IS THE WINNER..
                        GameManager.Instance.GameState = GameState.GameOver;
                    }
                }
            }
        }

        private int PlaceComparer(AircraftAgent a, AircraftAgent b)
        {
            return -a.ExpandProgress.CompareTo(b.ExpandProgress);
        }

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
                    _hudController.gameObject.SetActive(true);
                    foreach (var agent in _aircraftArea.AircraftAgents) agent.ThawAgent();
                    break;
                }
                case GameState.Paused:
                {
                    // Pause game time, freeze the agents
                    foreach (var agent in _aircraftArea.AircraftAgents) agent.FreezeAgent();
                    _pauseMenu.gameObject.SetActive(true);
                    break;
                }
                case GameState.GameOver:
                {
                    // Pause game time, hide UI & show GameOverUI, freeze the agents
                    foreach (var agent in _aircraftArea.AircraftAgents) agent.FreezeAgent();
                    _hudController.gameObject.SetActive(false);
                    _gameOverUI.gameObject.SetActive(true);
                    break;
                }
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
                    _expandProgress = 0,
                    _requiredMaterial = MaterialType.NoMaterial
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
        public MaterialType GetAgentRequiredMaterial(AircraftAgent agent)
        {
            //return _aircraftArea.Checkpoints[agent.NextCheckpointIndex];          Ig it can be used instead
            return _aircraftStatus[agent]._requiredMaterial;
        }
        
        
        public int GetAgentProgress(AircraftAgent agent)
        {
            return _aircraftStatus[agent]._expandProgress;
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
    }
}

