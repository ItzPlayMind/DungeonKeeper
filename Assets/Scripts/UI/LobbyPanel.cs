using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    public static LobbyPanel Instance;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    private class ClientReadyState
    {
        public ulong clientID;
        public UICheckbox readyStateObject;
    }
    [SerializeField] private Button startButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private RectTransform characterSelectionContentPanel;
    [SerializeField] private TMPro.TextMeshProUGUI playerText;
    [SerializeField] private GameObject characterPortraitPrefab;
    [SerializeField] private RectTransform[] characterCategory = new RectTransform[3];
    [SerializeField] private RectTransform[] characterSelection = new RectTransform[3];
    [SerializeField] private Animator[] redTeamStands = new Animator[3];
    [SerializeField] private Animator[] blueTeamStands = new Animator[3];

    [SerializeField] private UICheckbox readyStatePrefab;
    [SerializeField] private Transform readyStateTransform;
    private bool isLocalReady = false;

    private List<ClientReadyState> clientReadyStates = new List<ClientReadyState>();

    private int characterSelectionIndex = -1;

    public Button StartButton { get => startButton; }
    public Button ReadyButton { get => readyButton; }

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
        isLocalReady = false;
        characterSelectionPanel.SetActive(true);
        foreach (var portraits in Lobby.Instance.CharacterPortraits)
        {
            if(portraits.characterPotrait != null)
                portraits.characterPotrait.interactable = true;   
        }
    }

    private void OnDisable()
    {
        ClearClientReadyStates();
    }

    private void Start()
    {
        SetAndAddSizeForCategory(CharacterType.Tank);
        SetAndAddSizeForCategory(CharacterType.Damage);
        SetAndAddSizeForCategory(CharacterType.Support);
        for (int i = 0; i < Lobby.Instance.CharacterPortraits.Count; i++)
        {
            SetupPotraitWithIndex(i);
        }
    }
    private void SetAndAddSizeForCategory(CharacterType type)
    {
        Vector2 contentSize = characterSelectionContentPanel.sizeDelta;
        int characterLinesForSection = (int)Mathf.Ceil((Lobby.Instance.CharacterPortraits.Count(x => x.type == type) * 200) / (1476-40))+1;
        var category = characterCategory[(int)type];
        Vector2 size = category.sizeDelta;
        size.y = characterLinesForSection * 200+50;
        category.sizeDelta = size;
        contentSize.y += size.y-50;
        characterSelectionContentPanel.sizeDelta = contentSize;
    }

    private void SetupPotraitWithIndex(int index)
    {
        var portrait = Instantiate(characterPortraitPrefab, characterSelection[(int)Lobby.Instance.CharacterPortraits[index].type]);
        Lobby.Instance.CharacterPortraits[index].characterPotrait = portrait.GetComponent<Button>();
        portrait.transform.Find("Character").GetComponent<Image>().sprite = Lobby.Instance.CharacterPortraits[index].characterSprite;
        var hoverEvent = portrait.GetComponent<HoverEvent>();
        hoverEvent.onPointerEnter += () => AbilityHoverOver.Show(Lobby.Instance.CharacterPortraits[index].character.GetComponent<AbstractSpecial>());
        hoverEvent.onPointerExit += AbilityHoverOver.Hide;
        Lobby.Instance.CharacterPortraits[index].selectionObject = portrait.transform.Find("Selection").gameObject;
        if (index == characterSelectionIndex)
        {
            Lobby.Instance.CharacterPortraits[index].selectionObject.SetActive(true);
        }
        var buttonClick = new Button.ButtonClickedEvent();
        buttonClick.AddListener(() =>
        {
            if (isLocalReady) return;
            /*ColorBlock colorBlock = readyButton.colors;
            colorBlock.disabledColor = Color.green;
            readyButton.colors = colorBlock;*/
            readyButton.interactable = true;
            if (characterSelectionIndex >= 0)
                Lobby.Instance.CharacterPortraits[characterSelectionIndex].selectionObject.SetActive(false);
            characterSelectionIndex = index;
            Lobby.Instance.ChangeDisplayedCharacter(characterSelectionIndex);
            Lobby.Instance.CharacterPortraits[characterSelectionIndex].selectionObject.SetActive(true);
        });
        Lobby.Instance.CharacterPortraits[index].characterPotrait.onClick = buttonClick;
    }
   
    public void ChangeReady()
    {
        isLocalReady = !isLocalReady;
        Lobby.Instance.ChangeReadyState(characterSelectionIndex);
    }

    public void ChangeLockStateByIndex(int index, bool value)
    {
        Lobby.Instance.CharacterPortraits[index].locked = value;
        var button = Lobby.Instance.CharacterPortraits[index].characterPotrait.GetComponent<Button>();
        ColorBlock colorBlock = button.colors;
        colorBlock.disabledColor = Color.gray;
        button.colors = colorBlock;
        button.interactable= value;
        if(index == characterSelectionIndex && value)
        {
            var newIndex=(characterSelectionIndex+1)%Lobby.Instance.CharacterPortraits.Count;
            Lobby.Instance.CharacterPortraits[newIndex].characterPotrait.onClick?.Invoke();
        }
    }

    public void SetCharacterForClientIndex(int index, Lobby.Team team, int stanceIndex)
    {
        Animator animator = null;
        switch (team) {
            case Lobby.Team.Red:
                animator = redTeamStands[stanceIndex];
                break;
            case Lobby.Team.Blue:
                animator = blueTeamStands[stanceIndex];
                break;
        }
        if (animator != null)
            animator.runtimeAnimatorController = Lobby.Instance.CharacterPortraits[index].animations;
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
        Debug.Log(clientReadyStates.Count);
    }

    public void SetReadyState(ulong client, bool value)
    {
        var state = clientReadyStates.Find(x => x.clientID == client);
        if(state != null)
        {
            state.readyStateObject?.SetChecked(value);
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

    public void StartGame()
    {
        Lobby.Instance.StartGame();
    }

    public void StartStandsAttack()
    {
        readyButton.interactable = false;
        startButton.interactable = false;
        foreach (var item in redTeamStands)
        {
            if (item != null)
                item.SetTrigger("attacking");
        }
        foreach (var item in blueTeamStands)
        {
            if (item != null)
                item.SetTrigger("attacking");
        }
    }
}
