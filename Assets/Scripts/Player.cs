using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

[System.Serializable]
public class Player
{
    public string name;
    public long money;
    public List<County> countys;

    public Player(string name, long money)
    {
        this.name = name;
        this.money = money;
        countys = new List<County>();
    }
}
