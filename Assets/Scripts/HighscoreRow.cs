using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace NuttinToLose {
    public class HighscoreRow : MonoBehaviour {
        [SerializeField]
        TextMeshProUGUI placement = default;
        [SerializeField]
        TextMeshProUGUI nickname = default;
        [SerializeField]
        TextMeshProUGUI nuts = default;

        public void SetValues(int placement, string nickname, int nuts) {
            Assert.IsTrue(nuts >= 0);
            Assert.IsTrue(placement > 0);
            this.placement.text = placement.ToString();
            this.nickname.text = nickname;
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
}