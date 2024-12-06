using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    [System.Serializable]
    private class CharacterPortrait
    {
        public enum Type
        {
            Tank, Damage, Support
        }
        public GameObject character;
        public Type type;
        public Sprite characterSprite;
        //public AnimatorOverrideController characterAnimation;
        [HideInInspector] public GameObject characterPotrait;
        [HideInInspector] public GameObject selectionObject;
    }
    [SerializeField] private TMPro.TextMeshProUGUI playerText;
    [SerializeField] private GameObject characterPortraitPrefab;
    [SerializeField] private Transform[] characterSelection = new Transform[3];
    [SerializeField] private List<CharacterPortrait> characterPortraits = new List<CharacterPortrait>(); 

    private int characterSelectionIndex = 0;

    private void Start()
    {
        for (int i = 0; i < characterPortraits.Count; i++)
        {
            SetupPotraitWithIndex(i);
        }
    }

    private void SetupPotraitWithIndex(int index)
    {
        var portrait = Instantiate(characterPortraitPrefab, characterSelection[(int)characterPortraits[index].type]);
        characterPortraits[index].characterPotrait = portrait;
        portrait.transform.Find("Character").GetComponent<Image>().sprite = characterPortraits[index].characterSprite;
        characterPortraits[index].selectionObject = portrait.transform.Find("Selection").gameObject;
        if (index == characterSelectionIndex)
        {
            characterPortraits[index].selectionObject.SetActive(true);
        }
        var buttonClick = new Button.ButtonClickedEvent();
        buttonClick.AddListener(() =>
        {
            characterPortraits[characterSelectionIndex].selectionObject.SetActive(false);
            characterSelectionIndex = index;
            characterPortraits[characterSelectionIndex].selectionObject.SetActive(true);
        });
        portrait.GetComponent<Button>().onClick = buttonClick;
    }

    public GameObject GetCharacterByIndex(int index)
    {
        return characterPortraits[index].character;
    }

    public int GetSelectedIndex() => characterSelectionIndex;

    public void SetPlayerCount(int player)
    {
        playerText.text = "Players: " + player + "/6"; 
    }
}
