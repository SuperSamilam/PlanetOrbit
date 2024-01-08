using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public GameManager gameManager;
    List<Material> countryOrinalMat = new List<Material>();
    [SerializeField] Material hiddenMaterial;
    public int playerIndex;


    void Start()
    {
        List<int> tempCountrys = new List<int>();
        for (int i = 0; i < gameManager.counties.Count; i++)
        {
            tempCountrys.Add(i);
            countryOrinalMat.Add(gameManager.counties[i].obj.GetComponent<MeshRenderer>().material);
        }

        while (tempCountrys.Count != 0)
        {
            for (int i = 0; i < gameManager.players.Count; i++)
            {
                int r = Random.Range(0, tempCountrys.Count);
                gameManager.players[i].countys.Add(gameManager.counties[tempCountrys[r]]);
                tempCountrys.RemoveAt(r);

                if (tempCountrys.Count == 0)
                    break;
            }
        }
        nextPlayer();
    }
    public void nextPlayer()
    {
        playerIndex++;
        if (playerIndex == gameManager.players.Count)
            playerIndex = 0;

        gameManager.currentPlayer = gameManager.players[playerIndex];
        Debug.Log(gameManager.currentPlayer.name);
        gameManager.roadPlacement.BuildMesh(gameManager.currentPlayer.countys);
        gameManager.roadPlacement.BuildBridges();
        hideCountys();
    }

    public void hideCountys()
    {
        for (int i = 0; i < gameManager.counties.Count; i++)
        {
            if (gameManager.currentPlayer.countys.Contains(gameManager.counties[i]))
            {
                gameManager.counties[i].obj.GetComponent<MeshRenderer>().material = countryOrinalMat[i];
                if (gameManager.counties[i].obj.transform.childCount == 0)
                    continue;
                gameManager.counties[i].obj.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = countryOrinalMat[i];
                if (gameManager.counties[i].obj.transform.childCount == 1)
                    continue;
                for (int j = 1; j < gameManager.counties[i].obj.transform.childCount; j++)
                {
                    gameManager.counties[i].obj.transform.GetChild(j).gameObject.SetActive(true);
                }

            }
            else
            {
                if (gameManager.counties[i].obj.transform.childCount == 0)
                    continue;
                gameManager.counties[i].obj.GetComponent<MeshRenderer>().material = hiddenMaterial;
                if (gameManager.counties[i].obj.transform.childCount == 1)
                    continue;
                for (int j = 1; j < gameManager.counties[i].obj.transform.childCount; j++)
                {
                    gameManager.counties[i].obj.transform.GetChild(j).gameObject.SetActive(false);
                }
            }
        }
    }
}
