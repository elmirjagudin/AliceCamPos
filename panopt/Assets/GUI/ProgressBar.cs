using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public Button CancelButton;
    public Text stepLabel;
    public Image progressBar;

    public delegate void Cancel();
    Cancel CancelCB = null;

    public void CancelClicked()
    {
        if (CancelCB == null)
        {
            /* no cancel callback registered */
            return;
        }

        CancelCB();
    }

    public void Show(string stepName, Cancel CancelCB = null)
    {
        this.CancelCB = CancelCB;

        /* only show cancel button if cancel callback is provided */
        CancelButton.gameObject.SetActive(CancelCB != null);

        SetProgress(stepName, 0);
        gameObject.SetActive(true);
    }

    public void SetProgress(string caption, int completed, int total)
    {
        var msg = $"{caption} {completed}/{total}";
        var done = (float)completed/(float)total;
        SetProgress(msg, done);
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
