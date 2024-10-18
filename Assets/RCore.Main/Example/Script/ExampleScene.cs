using RCore.Example.Data.KeyValue;
using RCore.Example.UI;
using UnityEngine;

namespace RCore.Example
{
    public class ExampleScene : MonoBehaviour
    {
        private void Start()
        {
            ExampleKeyValueDBManager.Instance.Init();
            MainPanel.instance.Init();
            ExamplePoolsManager.Instance.Init();
        }
    }
}