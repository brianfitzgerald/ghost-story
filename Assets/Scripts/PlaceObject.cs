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

    public InputField textInput;

    public GameObject storyObjectListItemPrefab;

    public List<StoryObject> placeableStoryObjects = new List<StoryObject>();

    public GameObject storyObjectScrollContainer;


    private List<GameObject> placedStoryObjects = new List<GameObject>();

    public const string AR_OBJECT_TAG = "ARObject";

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
            else if (storyNodeController == null && selectedObject != null && selectedObject.GetComponent<StoryNodeController>() != null)
            {
                toggleStoryNodeDetail(false);
            }
        }
        else if (selectedObject != null)
        {
            toggleStoryNodeDetail(false);
        }
    }

    private void toggleStoryNodeDetail(bool focused, GameObject hitObj = null)
    {
        if (focused && hitObj != null && hitObj.GetComponent<StoryNodeController>() != null)
        {
            var nc = hitObj.GetComponent<StoryNodeController>();
            objectDetailParent.SetActive(true);
            selectedObject = hitObj;
            objectTitle.text = nc.storyNode.title;
            textInput.text = nc.storyNode.text;
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
