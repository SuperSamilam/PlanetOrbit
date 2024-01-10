using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int playerCount;
    public RoadPlacement roadPlacement;
    public TurnManager turnManager;
    public Player currentPlayer;

    //[HideInInspector]
    public List<Player> players = new List<Player>();

    //[HideInInspector]
    public List<County> counties = new List<County>();

    void Awake()
    {
        newGame();
    }

    void newGame()
    {
        for (int i = 1; i < playerCount; i++)
        {
            players.Add(new Player("Player " + i, 10000));
        }
        turnManager.playerIndex = -1;
    }






}
