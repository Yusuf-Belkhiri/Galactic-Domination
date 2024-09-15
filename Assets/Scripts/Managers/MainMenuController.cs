using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class MainMenuController : MonoBehaviour
    {
        #region FIELDS

        [SerializeField] private List<string> _levelScenesNames;
        // public TMP_Dropdown _levelDropdown;
        // public TMP_Dropdown _difficultyDropdown;
        private string _selectedLevel;
        private GameDifficulty _selectedDifficulty;
        
        #endregion


        #region METHODS

        private void Start()
        {
            // Default values
            _selectedLevel = _levelScenesNames[0];
            _selectedDifficulty = GameDifficulty.Normal;
        }

        
        // UI LISTENERS
        public void SetLevel(int levelIndex)
        {
            _selectedLevel = _levelScenesNames[levelIndex];
        }

        public void SetDifficulty(int difficultyIndex)
        {
            _selectedDifficulty = (GameDifficulty)difficultyIndex;
        }
        
        public void OnStartButtonClicked()
        {
            //GameManager.Instance.GameDifficulty = _selectedDifficulty;
            GameManager.Instance.LoadLevel(_selectedLevel, GameState.Preparing);
        }

        public void OnQuitButtonClicked()
        {
            Application.Quit();
        }

        #endregion
        
    }
    
}
