using System.Collections;
using TMPro;
using UnityEngine;

namespace Aircraft
{
    public class CountdownUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countdownText;
        
        
        public IEnumerator StartCountdown()
        {
            yield return new WaitForSeconds(0.5f);

            AudioManager.Instance.Play(AudioClipsNames.Announce4);
            _countdownText.fontSize /= 2;
            _countdownText.SetText("Get ready to launch into space");
            yield return new WaitForSeconds(5.5f);
            
            _countdownText.fontSize *= 2;
            _countdownText.SetText("3");
            AudioManager.Instance.Play(AudioClipsNames.Announce3);
            yield return new WaitForSeconds(1);
            _countdownText.SetText(string.Empty);
            yield return new WaitForSeconds(.5f);
            
            _countdownText.SetText("2");
            AudioManager.Instance.Play(AudioClipsNames.Announce2);
            yield return new WaitForSeconds(1);
            _countdownText.SetText(string.Empty);
            yield return new WaitForSeconds(.5f);
            
            _countdownText.SetText("1");
            AudioManager.Instance.Play(AudioClipsNames.Announce1);
            yield return new WaitForSeconds(1);
            _countdownText.SetText(string.Empty);
            yield return new WaitForSeconds(.5f);

            _countdownText.SetText("GO!");
            AudioManager.Instance.Play(AudioClipsNames.Announce0);
            yield return new WaitForSeconds(1);
            _countdownText.SetText(string.Empty);
        }
    }
}
