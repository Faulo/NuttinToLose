using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class HighscoreRow : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI placement = default;
    [SerializeField]
    TextMeshProUGUI nickname = default;
    [SerializeField]
    TextMeshProUGUI nuts = default;
    void Start()
    {
        placement.text = "x";
        nickname.text = "Nickname";
        nuts.text = "0";
    }

    public void SetValues(int placement, string name, int nuts) {
        Assert.IsTrue(nuts >= 0);
        Assert.IsTrue(placement > 0);
        this.placement.text = placement.ToString();
        nickname.text = name;
        this.nuts.text = nuts.ToString();
    }

    public void SetPlace(int placement) {
        Assert.IsTrue(placement > 0);
        this.placement.text = placement.ToString();
    }

    public void SetNickName(string nickname) {
        this.nickname.text = nickname;
    }

    public void SetNuts(int nuts) {
        Assert.IsTrue(nuts >= 0);
        this.nuts.text = nuts.ToString();
    }
}
