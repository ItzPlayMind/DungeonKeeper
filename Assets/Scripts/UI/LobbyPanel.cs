using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        public bool locked;
        //public AnimatorOverrideController characterAnimation;
        [HideInInspector] public Button characterPotrait;
        [HideInInspector] public GameObject selectionObject;
    }

    private class ClientReadyState
    {
        public ulong clientID;
        public UICheckbox readyStateObject;
    }

    [SerializeField] private GameObject teamPanel;
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private Transform[] redTeamPlayerCards = new Transform[3];
    [SerializeField] private Transform[] blueTeamPlayerCards = new Transform[3];
    [SerializeField] private TMPro.TextMeshProUGUI playerText;
    [SerializeField] private GameObject characterPortraitPrefab;
    [SerializeField] private Transform[] characterSelection = new Transform[3];
    [SerializeField] private List<CharacterPortrait> characterPortraits = new List<CharacterPortrait>();
    [SerializeField] private UICheckbox readyStatePrefab;
    [SerializeField] private Transform readyStateTransform;
    private bool isLocalReady = false;

    private List<ClientReadyState> clientReadyStates = new List<ClientReadyState>();

    private int characterSelectionIndex = 0;

    public void SwitchToCharacterSelection()
    {
        characterSelectionPanel.SetActive(true);
        teamPanel.SetActive(false);
    }

    public void ResetOnDisconnect()
    {
        clientReadyStates = new List<ClientReadyState>();
    }

    public void UpdateTeamPanel(ulong[] redTeam, ulong[] blueTeam)
    {
        foreach (var item in redTeamPlayerCards)
            item.gameObject.SetActive(false);
        foreach (var item in blueTeamPlayerCards)
            item.gameObject.SetActive(false);

        for (int i = 0; i < redTeam.Length; i++)
        {
            redTeamPlayerCards[i].gameObject.SetActive(true);
            redTeamPlayerCards[i].GetComponentInChildren<TMPro.TextMeshProUGUI>().text = redTeam[i].ToString();
        }
        for (int i = 0; i < blueTeam.Length; i++)
        {
            blueTeamPlayerCards[i].gameObject.SetActive(true);
            blueTeamPlayerCards[i].GetComponentInChildren<TMPro.TextMeshProUGUI>().text = blueTeam[i].ToString();
        }
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
        isLocalReady = false;
        teamPanel.SetActive(true);
        characterSelectionPanel.SetActive(false);
        foreach (var portraits in characterPortraits)
        {
            if(portraits.characterPotrait != null)
                portraits.characterPotrait.interactable = true;   
        }
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
        characterPortraits[index].characterPotrait = portrait.GetComponent<Button>();
        portrait.transform.Find("Character").GetComponent<Image>().sprite = characterPortraits[index].characterSprite;
        characterPortraits[index].selectionObject = portrait.transform.Find("Selection").gameObject;
        if (index == characterSelectionIndex)
        {
            characterPortraits[index].selectionObject.SetActive(true);
        }
        var buttonClick = new Button.ButtonClickedEvent();
        buttonClick.AddListener(() =>
        {
            if (isLocalReady) return;
            characterPortraits[characterSelectionIndex].selectionObject.SetActive(false);
            characterSelectionIndex = index;
            characterPortraits[characterSelectionIndex].selectionObject.SetActive(true);
        });
        characterPortraits[index].characterPotrait.onClick = buttonClick;
    }
   
    public void ChangeReady()
    {
        isLocalReady = !isLocalReady;
        GameManager.instance.ChangeReadyState(characterSelectionIndex);
    }

    public void ChangeLockStateByIndex(int index, bool value)
    {
        characterPortraits[index].locked = value;
        characterPortraits[index].characterPotrait.GetComponent<Button>().interactable=!value;
        if(index == characterSelectionIndex && value)
        {
            var newIndex=(characterSelectionIndex+1)%characterPortraits.Count;
            characterPortraits[newIndex].characterPotrait.onClick?.Invoke();
        }
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
