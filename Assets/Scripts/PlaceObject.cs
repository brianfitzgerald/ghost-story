using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[Serializable]
public struct StoryObject
{
    public string name;
    public GameObject prefab;
    public float overheadUIOffset;
}

[RequireComponent(typeof(ARSessionOrigin))]
public class PlaceObject : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject m_PlacedPrefab;

    /// <summary>
    /// The prefab to instantiate on touch.
    /// </summary>
    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }


    /// <summary>
    /// Invoked whenever an object is placed in on a plane.
    /// </summary>
    public static event Action onPlacedObject;

    ARSessionOrigin m_SessionOrigin;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    Vector3 screenPosition;


    public Camera ARCamera;

    public Text objectTitle;

    private GameObject selectedObject;

    public GameObject objectDetailParent;

    public GameObject playModeOverheadPrefab;

    public InputField textInput;

    public GameObject storyObjectListItemPrefab;

    public List<StoryObject> placeableStoryObjects = new List<StoryObject>();

    public GameObject storyObjectScrollContainer;

    public GameObject playButton;


    private List<GameObject> placedStoryObjects = new List<GameObject>();

    public const string AR_OBJECT_TAG = "ARObject";

    private bool inPlayMode = false;

    void Awake()
    {
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        print(Screen.width);
        screenPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        print(screenPosition);
        textInput.onValueChanged.AddListener(storyInputChanged);
        objectTitle.text = "";
        foreach (var storyObject in placeableStoryObjects)
        {
            var o = Instantiate(storyObjectListItemPrefab, storyObjectScrollContainer.transform);
            o.transform.GetChild(0).GetComponent<Text>().text = storyObject.name;
            o.GetComponent<Button>().onClick.AddListener(delegate
            {
                placeObject(storyObject);
            });
        }
        toggleStoryNodeDetail(false);
        objectDetailParent.transform.Find("Delete").GetComponent<Button>().onClick.AddListener(delegate
        {
            objectDetailParent.SetActive(false);
            Destroy(selectedObject);
        });
        playButton.GetComponent<Button>().onClick.AddListener(delegate
        {
            togglePlayMode();
        });

    }

    private void togglePlayMode()
    {
        toggleStoryNodeDetail(false);
        inPlayMode = !inPlayMode;
        var playButtonText = playButton.transform.GetChild(0).GetComponent<Text>();
        if (inPlayMode)
        {
            playButtonText.text = "Back";
        }
        else
        {
            playButtonText.text = "Play";
        }
    }

    private void placeObject(StoryObject storyObject)
    {
        var ray = ARCamera.ScreenPointToRay(screenPosition);
        Debug.DrawRay(ray.origin, ray.direction * 10000, Color.yellow);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            var spawnedObject = Instantiate(storyObject.prefab, hit.point, hit.collider.transform.rotation);
            spawnedObject.AddComponent(typeof(StoryNodeController));
            spawnedObject.GetComponent<StoryNodeController>().storyNode.title = storyObject.name;
            if (storyObject.overheadUIOffset != 0.0f)
            {
                spawnedObject.GetComponent<StoryNodeController>().overheadUIOffset = storyObject.overheadUIOffset;
            }
            var s = .25f;
            spawnedObject.transform.localScale = new Vector3(s, s, s);

            toggleStoryNodeDetail(true, spawnedObject);

            placedStoryObjects.Add(spawnedObject);

            if (onPlacedObject != null)
            {
                onPlacedObject();
            }
        }
    }

    private void storyInputChanged(string text)
    {
        if (!selectedObject)
        {
            return;
        }
        var storyController = selectedObject.GetComponent<StoryNodeController>();
        if (storyController != null)
        {
            storyController.storyNode.text = text;
        }
    }

    void Update()
    {
        var ray = ARCamera.ScreenPointToRay(screenPosition);
        Debug.DrawRay(ray.origin, ray.direction * 10000, Color.yellow);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            var hitObj = hit.transform.gameObject;
            var storyNodeController = hitObj.GetComponent<StoryNodeController>();
            if (storyNodeController != null && !GameObject.ReferenceEquals(hitObj, selectedObject) && hitObj.tag != AR_OBJECT_TAG)
            {
                toggleStoryNodeDetail(true, hitObj);
            }
            else if (storyNodeController == null && selectedObject != null && selectedObject.GetComponent<StoryNodeController>() != null && !textInput.isFocused)
            {
                toggleStoryNodeDetail(false);
            }
        }
        else if (selectedObject != null && !textInput.isFocused)
        {
            toggleStoryNodeDetail(false);
        }
    }

    private const string PLAY_MODE_OVERHEAD = "Play Mode Overhead";

    private void toggleStoryNodeDetail(bool focused, GameObject hitObj = null)
    {
        if (focused && hitObj != null && hitObj.GetComponent<StoryNodeController>() != null)
        {
            var nc = hitObj.GetComponent<StoryNodeController>();
            if (inPlayMode)
            {
                if (hitObj.transform.Find(PLAY_MODE_OVERHEAD) == null)
                {
                    var objectDetail = Instantiate(playModeOverheadPrefab, hitObj.transform);
                    // objectDetail.transform.Rotate(0, 180, 0);
                    objectDetail.transform.Translate(0, nc.overheadUIOffset, 0);
                    objectDetail.transform.GetChild(0).Find("Title").GetComponent<Text>().text = nc.storyNode.title;
                    objectDetail.transform.GetChild(0).Find("Text").GetComponent<Text>().text = nc.storyNode.text;
                }
            }
            else
            {
                objectDetailParent.SetActive(true);
                selectedObject = hitObj;
                objectTitle.text = nc.storyNode.title;
                textInput.text = nc.storyNode.text;
            }
        }
        else
        {
            if (inPlayMode)
            {
                if (hitObj != null && hitObj.GetComponent<StoryNodeController>() != null && hitObj.transform.childCount > 0)
                {
                    Destroy(hitObj.transform.Find(PLAY_MODE_OVERHEAD));
                }
            }
            else
            {
                objectTitle.text = "";
                objectDetailParent.SetActive(false);
                selectedObject = null;
                textInput.text = "";
            }
        }
    }
}
