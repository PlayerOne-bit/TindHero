using UnityEngine;
using TMPro;
using System.Collections;

public class MobileInputFix : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField[] inputFields;

    [Header("Settings")]
    public TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;

    private TouchScreenKeyboard keyboard;
    private TMP_InputField activeField;

    void Start()
    {
        if (inputFields == null || inputFields.Length == 0)
            inputFields = GetComponentsInChildren<TMP_InputField>();

        foreach (var field in inputFields)
        {
            if (field == null) continue;
            field.onSelect.AddListener(OnInputSelected);
            field.onDeselect.AddListener(OnInputDeselected);
        }
    }

    void Update()
    {
        if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Done)
        {
            if (activeField != null)
                activeField.text = keyboard.text;
        }
    }

    void OnInputSelected(string value)
    {
        Debug.Log("INPUT SELECTED");
        StartCoroutine(HandleKeyboard());
    }

    IEnumerator HandleKeyboard()
    {
        // Wait a frame for focus to actually set
        yield return new WaitForEndOfFrame();

        // Find focused field after waiting
        foreach (var field in inputFields)
        {
            if (field != null && field.isFocused)
            {
                activeField = field;
                Debug.Log("Active field found: " + field.name);
                break;
            }
        }

        if (activeField == null)
        {
            Debug.Log("No focused field found!");
            yield break;
        }

#if UNITY_ANDROID || UNITY_IOS
        Debug.Log("isSupported: " + TouchScreenKeyboard.isSupported);
        Debug.Log("activeField null? " + (activeField == null));

        if (TouchScreenKeyboard.isSupported)
        {
            Debug.Log("Opening keyboard...");
            keyboard = TouchScreenKeyboard.Open(
                activeField.text,
                keyboardType,
                false,
                activeField.multiLine,
                activeField.inputType == TMP_InputField.InputType.Password
            );
            Debug.Log("Keyboard object null? " + (keyboard == null));
            if (keyboard != null)
                Debug.Log("Keyboard status: " + keyboard.status);
        }
#endif
    }

    void OnInputDeselected(string value)
    {
        Debug.Log("DESELECTED: " + (activeField != null ? activeField.name : "null") +
                  " | Stack: " + System.Environment.StackTrace);

#if UNITY_ANDROID || UNITY_IOS
        if (keyboard != null && keyboard.active)
            keyboard.active = false;
#endif
        activeField = null;
    }

    IEnumerator ReactivateAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        if (activeField != null)
        {
            activeField.ActivateInputField();
            activeField.Select();
        }
    }

    void OnDestroy()
    {
        foreach (var field in inputFields)
        {
            if (field == null) continue;
            field.onSelect.RemoveListener(OnInputSelected);
            field.onDeselect.RemoveListener(OnInputDeselected);
        }
    }
}