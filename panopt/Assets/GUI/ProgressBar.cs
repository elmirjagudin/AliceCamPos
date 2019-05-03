using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public delegate void Cancel();

    Cancel CancelCB = null;

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

    public void CancelClicked()
    {
        if (CancelCB == null)
        {
            /* no cancel callback registered */
            return;
        }

        CancelCB();
    }

    public void Show(string stepName, Cancel CancelCB)
    {
        this.CancelCB = CancelCB;
        SetProgress(stepName, 0);
        gameObject.SetActive(true);
    }

    public void SetProgress(string stepName, float done)
    {
        stepLabel.text = stepName;
        progressBar.fillAmount = done;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        this.CancelCB = null;
    }
}
