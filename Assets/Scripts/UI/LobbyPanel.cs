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

    private class ClientReadyState
    {
        public ulong clientID;
        public UICheckbox readyStateObject;
    }

    [SerializeField] private TMPro.TextMeshProUGUI playerText;
    [SerializeField] private GameObject characterPortraitPrefab;
    [SerializeField] private Transform[] characterSelection = new Transform[3];
    [SerializeField] private List<CharacterPortrait> characterPortraits = new List<CharacterPortrait>();
    [SerializeField] private UICheckbox readyStatePrefab;
    [SerializeField] private Transform readyStateTransform;
    
    private List<ClientReadyState> clientReadyStates = new List<ClientReadyState>();

    private int characterSelectionIndex = 0;

    public void ResetOnDisconnect()
    {
        clientReadyStates = new List<ClientReadyState>();
    }

    public void ClearClientReadyStates()
    {
        for (int i = 0; i < readyStateTransform.childCount; i++)
        {
            Destroy(readyStateTransform.GetChild(i).gameObject);
        }
    }

    private void OnEnable()
    {
        ClearClientReadyStates();
    }

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

    public void AddReadyState(ulong client)
    {
        var newReadyState = Instantiate(readyStatePrefab, readyStateTransform);
        clientReadyStates.Add(new ClientReadyState()
        {
            clientID = client,
            readyStateObject = newReadyState
        });
    }

    public void SetReadyState(ulong client, bool value)
    {
        var state = clientReadyStates.Find(x => x.clientID == client);
        if(state != null)
        {
            state.readyStateObject.SetChecked(value);
        }
    }

    public void RemoveReadyState(ulong client)
    {
        var state = clientReadyStates.Find(x => x.clientID == client);
        if (state != null)
        {
            clientReadyStates.Remove(state);
            Destroy(state.readyStateObject.gameObject);
        }
    }
}
