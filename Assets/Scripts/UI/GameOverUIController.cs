using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aircraft
{
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _placeText;
        [FormerlySerializedAs("_raceManager")] [SerializeField] private GameplayManager _gameplayManager;


        // Show finish place text
        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.GameState == GameState.GameOver)
            {
                string place = _gameplayManager.GetAgentPlace(_gameplayManager.FollowedAgent);
                _placeText.SetText($"{place} Place");
            }
        }

        public void MainMenuButtonClicked()
        {
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
        }
    }
}