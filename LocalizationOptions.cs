using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using TMPro;
using UnityEngine.Audio;

//manages the languagues in the options menu, converting the Enum's to strings and looking them up in an XML document to get the proper translations once switched
public class LocalizationOptions : MonoBehaviour
{

    public AudioMixer audioMixer;
    float volume;

    public enum LanguageOptions
    {
        English,
        Français,
        Español,
        Italiano,
        Deutsch,
        Русский,
        日本語,
        한국어,
        中文
    }

    public static LanguageOptions activeLanguage = LanguageOptions.English;

    public TMP_Dropdown languageDropdown;

    void Start()
    {
        languageDropdown.AddOptions(GetLocalizationNames());

        audioMixer.GetFloat("volume", out volume);
        gameObject.GetComponentInChildren<Slider>().value = volume;

        languageDropdown.value = (int) activeLanguage;
    }

    public void SetVolume (float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }

    public void HandleInputData(int value)
    {
        switch (value)
        {
            case (int)LanguageOptions.English:
                LocalisationScript.language = LocalisationScript.Language.English;
                break;
            case (int)LanguageOptions.Français:
                LocalisationScript.language = LocalisationScript.Language.French;
                break;
            case (int)LanguageOptions.Español:
                LocalisationScript.language = LocalisationScript.Language.Spanish;
                break;
            case (int)LanguageOptions.Русский:
                LocalisationScript.language = LocalisationScript.Language.Russian;
                break;
            case (int)LanguageOptions.日本語:
                LocalisationScript.language = LocalisationScript.Language.Japanese;
                break;
            case (int)LanguageOptions.한국어:
                LocalisationScript.language = LocalisationScript.Language.Korean;
                break;
            case (int)LanguageOptions.中文:
                LocalisationScript.language = LocalisationScript.Language.Chinese;
                break;
            case (int)LanguageOptions.Italiano:
                LocalisationScript.language = LocalisationScript.Language.Italian;
                break;
            case (int)LanguageOptions.Deutsch:
                LocalisationScript.language = LocalisationScript.Language.German;
                break;
            default:
                LocalisationScript.language = LocalisationScript.Language.English;
                break;
        }

        activeLanguage = (LanguageOptions)value;

        LocalizeTextMesh[] toBeLocalized = FindObjectsOfType<LocalizeTextMesh>();
        foreach (LocalizeTextMesh text in toBeLocalized)
        {
            text.updateLocalization();
        }
    }

    public List<string> GetLocalizationNames()
    {
        return Enum.GetNames(typeof(LanguageOptions)).ToList();
    }
}
