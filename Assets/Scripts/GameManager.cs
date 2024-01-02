using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int playerCount;
    public Material hiddenMaterial;

    //[HideInInspector]
    public List<Player> players = new List<Player>();

    //[HideInInspector]
    public List<County> counties = new List<County>();
    List<Material> countryOrinalMat = new List<Material>();

    void Awake()
    {
        List<int> tempCountrys = new List<int>();
        for (int i = 0; i < counties.Count; i++)
        {
            tempCountrys.Add(i);
            countryOrinalMat.Add(counties[i].obj.GetComponent<MeshRenderer>().material);
        }

        for (int i = 0; i < playerCount; i++)
        {
            players.Add(new Player("Player " + i, 10000));
        }

        while (tempCountrys.Count != 0)
        {
            for (int i = 0; i < players.Count; i++)
            {
                int r = Random.Range(0, tempCountrys.Count);
                players[i].countys.Add(counties[tempCountrys[r]]);
                tempCountrys.RemoveAt(r);

                if (tempCountrys.Count == 0)
                    break;
            }
        }

        currentPlayer = players[0];
        playerIndex = 0;
        hideCountys();
    }

    Player currentPlayer;
    int playerIndex;

    public void nextPlayer()
    {
        playerIndex++;
        if (playerIndex == players.Count)
            playerIndex = 0;

        currentPlayer = players[playerIndex];
        Debug.Log(currentPlayer.name);
        hideCountys();
    }

    void hideCountys()
    {
        for (int i = 0; i < counties.Count; i++)
        {
            if (currentPlayer.countys.Contains(counties[i]))
            {
                counties[i].obj.GetComponent<MeshRenderer>().material = countryOrinalMat[i];
                for (int j = 0; j < counties[i].obj.transform.childCount; j++)
                {
                    counties[i].obj.transform.GetChild(j).gameObject.GetComponent<MeshRenderer>().material = countryOrinalMat[i];
                }
            }
            else
            {
                counties[i].obj.GetComponent<MeshRenderer>().material = hiddenMaterial;
                for (int j = 0; j < counties[i].obj.transform.childCount; j++)
                {
                    counties[i].obj.transform.GetChild(j).gameObject.GetComponent<MeshRenderer>().material = hiddenMaterial;
                }
            }
        }
    }


}
