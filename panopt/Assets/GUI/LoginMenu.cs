using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Hagring;

public class LoginMenu : MonoBehaviour
{
    public enum Error { Authentication, Connection};

    public delegate void LoginError(Error error);
    public static event LoginError LoginErrorEvent;

    public delegate void LoggedIn(CloudAPI.Model[] models);
    public static event LoggedIn LoggedInEvent;

    public InputField Username;
    public InputField Password;
    public Button LoginButton;

    void SetText(InputField field, string text)
    {
        if (text == null)
        {
            return;
        }

        field.text = text;
    }

    void Start()
    {
        SetText(Username, Persisted.Username);
        SetText(Password, Persisted.Password);

        /*
         * put the input caret in the username text field,
         * so that user can start typing username without first clicking
         */
        Username.ActivateInputField();
    }

    /*
     * password or username input field changed
     */
    public void FieldChanged()
    {
        var EnableLogin = Username.text.Length > 0 && Password.text.Length > 0;
        LoginButton.interactable = EnableLogin;
    }

    void SetWidgetsInteractable(bool interactable)
    {
        Username.interactable = interactable;
        Password.interactable = interactable;
        LoginButton.interactable = interactable;
    }

    public void LoginClicked()
    {
        SetWidgetsInteractable(false);
        Utils.StartThread(Login, "Login");
    }

    void DispatchLoginError(Error error)
    {
        MainThreadRunner.Run(delegate ()
        {
            ErrorMessage.ClosedEvent += HandleErrorMessageClosed;
            gameObject.SetActive(false);
            LoginErrorEvent?.Invoke(error);
        });
    }

    void HandleErrorMessageClosed()
    {
        ErrorMessage.ClosedEvent -= HandleErrorMessageClosed;
        gameObject.SetActive(true);
    }


    void LoginSucessfull(CloudAPI.Model[] models)
    {
        Persisted.Username = Username.text;
        Persisted.Password = Password.text;

        gameObject.SetActive(false);
        LoggedInEvent?.Invoke(models);
    }

    void Login()
    {
        try
        {
            CloudAPI.Instance.SetUserCredentials(Username.text, Password.text);
            var mods = CloudAPI.Instance.GetModels();
            MainThreadRunner.Run(() => LoginSucessfull(mods));
        }
        catch (AuthenticationError)
        {
            DispatchLoginError(Error.Authentication);
        }
        catch (ConnectionError)
        {
            DispatchLoginError(Error.Connection);
        }
        finally
        {
            MainThreadRunner.Run(() => SetWidgetsInteractable(true));
        }
    }

    void HandleTabKey()
    {
        EventSystem eventSystem = EventSystem.current;

        Selectable next = null;
        if (eventSystem.currentSelectedGameObject != null) /* handle case where no widget is selected */
        {
            next = eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
        }

        /* either select 'next' or goto username if no next is found */
        GameObject select = next != null ?
            next.gameObject :
            Username.gameObject;
        eventSystem.SetSelectedGameObject(select);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleTabKey();
        }
    }
}
