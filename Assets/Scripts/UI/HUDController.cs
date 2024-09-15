using System.Collections.Generic;
using Aircraft;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _placeText;
    [FormerlySerializedAs("_timeText")] [SerializeField] private TextMeshProUGUI _requiredMaterialText;
    [FormerlySerializedAs("_lapText")] [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private Image _checkpointIcon;
    [SerializeField] private Image _checkpointArrow;

    [SerializeField] private float _indicatorLimit = .7f;       // just like padding
    private float _halfLimit;
    public AircraftAgent FollowedAgent { get; set; }        // The agent this UI shows info for
    [FormerlySerializedAs("_raceManager")] [SerializeField] private GameplayManager _gameplayManager;

    private bool _showArrow;
    private void Start()
    {
        _halfLimit = _indicatorLimit / 2f;

        _countdownRemainingTime = _countdownTime;
    }

    private void Update()
    {
        if (GameManager.Instance.GameState == GameState.Playing)
        {
            _countdownRemainingTime -= Time.deltaTime;
            if (_countdownRemainingTime <= 0)
            {
                _countdownRemainingTime = _countdownTime;
            }
            
            _countdownText.SetText($"{_countdownRemainingTime:0.0} s");
        }
        
        if (FollowedAgent != null)
        {
            UpdatePlaceText();
            UpdateRequiredMaterialText();
            UpdateProgressText();
           
            //UpdateArrow();
        }
    }

    private void UpdatePlaceText()
    {
        string place = _gameplayManager.GetAgentPlace(FollowedAgent);
        _placeText.SetText($"You are the {place}");
    }

    private void UpdateRequiredMaterialText()
    {
        var materialType = _gameplayManager.GetAgentRequiredMaterial(FollowedAgent);
        _requiredMaterialText.SetText($"Search for {materialType}");         // time.ToString(0.0) format
    }

    private void UpdateProgressText()
    {
        var progress = _gameplayManager.GetAgentProgress(FollowedAgent);
        _progressText.SetText($"Expansion Progress: {progress}%");
    }

    /*private void UpdateArrow()
    {
        Transform nextCheckpoint = _gameplayManager.GetAgentCheckpoint(FollowedAgent);
        Vector3 viewportPoint = _gameplayManager.ActiveCamera.WorldToViewportPoint(nextCheckpoint.position);        // Viewport: [0,0] (bottom left) to [1,1], the z position is in world units from the camera,
                                                                                                                // Viewport space is a normalization that can be useful to check if something is on screen
        bool behindCamera = viewportPoint.z < 0;
        viewportPoint.z = 0;
        
        // Do position calculations 
        Vector3 viewportCenter = new Vector3(.5f, .5f, 0f);
        Vector3 fromCenter = viewportPoint - viewportCenter;        // the vector from the center to the point
        _showArrow = false;

        if (behindCamera)
        {
            fromCenter = -fromCenter.normalized * _halfLimit;     // limit distance from the center, Viewport point is flipped when object is behind camera
            _showArrow = true;
        }
        else
        {
            if (fromCenter.magnitude > _halfLimit)
            {
                fromCenter = fromCenter.normalized * _halfLimit;    // limit distance from the center
                _showArrow = true;
            }
        }
        // Update the checkpoint icon and arrow
        _checkpointArrow.gameObject.SetActive(_showArrow);
        _checkpointArrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, fromCenter);
        _checkpointIcon.rectTransform.position =
            _gameplayManager.ActiveCamera.ViewportToScreenPoint(fromCenter + viewportCenter);
    }*/



    [SerializeField] private TextMeshProUGUI _countdownText;
    private float _countdownTime = 10f;

    private float _countdownRemainingTime;
    /*public IEnumerable<WaitForSeconds> StartCountdown()
    {
        /*_countdownText.SetText("3");
        yield return new WaitForSeconds(1);
        _countdownText.SetText(string.Empty);
        yield return new WaitForSeconds(.5f);
            
        _countdownText.SetText("2");
        yield return new WaitForSeconds(1);
        _countdownText.SetText(string.Empty);
        yield return new WaitForSeconds(.5f);
            
        _countdownText.SetText("1");
        yield return new WaitForSeconds(1);
        _countdownText.SetText(string.Empty);
        yield return new WaitForSeconds(.5f);

        _countdownText.SetText("GO!");
        yield return new WaitForSeconds(1);
        _countdownText.SetText(string.Empty);#1#

    }*/
}
