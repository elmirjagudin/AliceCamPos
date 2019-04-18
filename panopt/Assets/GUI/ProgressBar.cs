using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    Text _stepLabel = null;
    Text stepLabel
    {
        get
        {
            if (_stepLabel == null)
            {
                _stepLabel = gameObject.GetComponentInChildren<Text>();
            }
            return _stepLabel;
        }
    }
    Image _progressBar;
    Image progressBar
    {
        get
        {
            if (_progressBar == null)
            {
                _progressBar = gameObject.GetComponentInChildren<Image>();
            }
            return _progressBar;
        }
    }

    public void SetProgress(string stepName = "", float done = 0)
    {
        gameObject.SetActive(true);
        stepLabel.text = stepName;
        progressBar.fillAmount = done;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
