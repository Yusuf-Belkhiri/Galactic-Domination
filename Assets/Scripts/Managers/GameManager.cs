using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aircraft
{
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Paused,
        GameOver
    }

    public enum GameDifficulty
    {
        Normal,
        Hard
    }
    
    public delegate void OnStateChangeHandler(GameState newState);
    
    /// <summary>
    /// GameManager
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region FIELDS

        public event OnStateChangeHandler OnStateChange;

        private GameState _gameState;
        public GameState GameState
        {
            get => _gameState;
            set     // may change to non private
            {
                _gameState = value;
                if (OnStateChange != null) OnStateChange(_gameState);
            }
        }
        
        public GameDifficulty GameDifficulty { get; set; }
        
        // Singleton
        public static GameManager Instance { get; private set; }

        #endregion


        #region METHODS

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }


        public void LoadLevel(string levelName, GameState newState)
        {
            StartCoroutine(LoadLevelAsync(levelName, newState));
        }
        private IEnumerator LoadLevelAsync(string levelName, GameState newState)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(levelName);
            while (!operation.isDone)
            {
                // Add looping code before scene load here
                yield return null;
            }
            GameState = newState;
        }
        #endregion
        
    }
}
