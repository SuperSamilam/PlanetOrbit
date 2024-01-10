using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public GameManager gameManager;
    List<Material> countryOrinalMat = new List<Material>();
    [SerializeField] Material hiddenMaterial;
    [SerializeField] Slider slider;
    public int playerIndex;
    public float timer;
    public float maxTime = 90f;


    void Start()
    {
        slider.maxValue = maxTime;
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

    void Update()
    {
        if (timer < maxTime)
        {
            timer += Time.deltaTime;
            slider.value = timer;
        }
        else
        {
            nextPlayer();
        }
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
        timer = 0;
        hideCountys();
    }

    public void hideCountys()
    {
        for (int i = 0; i < gameManager.counties.Count; i++)
        {
            if (gameManager.currentPlayer.countys.Contains(gameManager.counties[i]))
            {
                gameManager.counties[i].obj.GetComponent<MeshRenderer>().material = countryOrinalMat[i];
                for (int j = 0; j < gameManager.counties[i].obj.transform.childCount; j++)
                {
                    if (gameManager.counties[i].obj.transform.GetChild(j).gameObject.name.Contains("Edge"))
                        gameManager.counties[i].obj.transform.GetChild(j).GetComponent<MeshRenderer>().material = countryOrinalMat[i];
                    else
                        gameManager.counties[i].obj.transform.GetChild(j).gameObject.SetActive(true);
                }
            }
            else
            {
                gameManager.counties[i].obj.GetComponent<MeshRenderer>().material = hiddenMaterial;
                for (int j = 0; j < gameManager.counties[i].obj.transform.childCount; j++)
                {
                    if (gameManager.counties[i].obj.transform.GetChild(j).gameObject.name.Contains("Edge"))
                        gameManager.counties[i].obj.transform.GetChild(j).GetComponent<MeshRenderer>().material = hiddenMaterial;
                    else
                        gameManager.counties[i].obj.transform.GetChild(j).gameObject.SetActive(false);
                }
            }
        }
    }
}
