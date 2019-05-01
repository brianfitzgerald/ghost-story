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

    private GameObject selectedObject;

    public InputField textInput;

    public GameObject storyObjectListItemPrefab;

    public List<StoryObject> placeableStoryObjects = new List<StoryObject>();

    public GameObject storyObjectScrollContainer;

    private List<GameObject> placedStoryObjects = new List<GameObject>();

    void Awake()
    {
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        screenPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        textInput.onValueChanged.AddListener(storyInputChanged);
        textInput.gameObject.SetActive(false);
        foreach (var storyObject in placeableStoryObjects)
        {
            var o = Instantiate(storyObjectListItemPrefab, storyObjectScrollContainer.transform);
            o.transform.GetChild(0).GetComponent<Text>().text = storyObject.name;
            o.GetComponent<Button>().onClick.AddListener(delegate
            {
                placeObject(storyObject);
            });
        }
    }

    private void placeObject(StoryObject storyObject)
    {
        if (m_SessionOrigin.Raycast(screenPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = s_Hits[0].pose;

            var spawnedObject = Instantiate(storyObject.prefab, hitPose.position, hitPose.rotation);
            spawnedObject.AddComponent(typeof(StoryNodeController));
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
            var h = hitObj.GetComponent<StoryNodeController>();
            if (h != null && !GameObject.ReferenceEquals(hitObj, selectedObject))
            {
                toggleStoryNodeDetail(true, hitObj);
            }
            if (h == null && selectedObject != null && selectedObject.GetComponent<StoryNodeController>() != null)
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
            print("set active");
            textInput.gameObject.SetActive(true);
            selectedObject = hitObj;
            textInput.text = hitObj.GetComponent<StoryNodeController>().storyNode.text;
        }
        else
        {
            print("set inactive");
            textInput.gameObject.SetActive(false);
            selectedObject = null;
            textInput.text = "";
        }
    }
}
