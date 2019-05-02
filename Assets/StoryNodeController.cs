using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StoryNode
{
    [TextArea]
    public string text;
    public string title;
}

public class StoryNodeController : MonoBehaviour
{
    public StoryNode storyNode = new StoryNode();
    // Start is called before the first frame update
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
