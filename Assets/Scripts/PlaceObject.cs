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

    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }


    public static event Action onPlacedObject;

    ARSessionOrigin m_SessionOrigin;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    Vector3 screenPosition;

    public Camera ARCamera;

    public Text objectTitle;

    private GameObject selectedObject;

    public GameObject objectDetailParent;

    public GameObject playModeOverheadPrefab;

    public Button increaseOrderButton;
    public Button decreaseOrderButton;
    public Text storyOrderText;

    public InputField textInput;

    public GameObject storyObjectListItemPrefab;

    public GameObject storyObjectScrollContainer;

    public GameObject playButton;


    public List<StoryObject> placeableStoryObjects = new List<StoryObject>();


    private List<GameObject> placedStoryObjects = new List<GameObject>();

    private int currentPlayModeObjectIndex = 0;

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
        storyOrderText.text = "";
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
        increaseOrderButton.onClick.AddListener(delegate
        {
            changeObjectStoryOrder(true, selectedObject);
        });
        decreaseOrderButton.onClick.AddListener(delegate
        {
            changeObjectStoryOrder(false, selectedObject);
        });

    }

    private void changeObjectStoryOrder(bool increase, GameObject storyObject)
    {
        var placedIndex = placedStoryObjects.IndexOf(storyObject);
        var j = increase ? placedIndex + 1 : placedIndex - 1;
        if (j > placedStoryObjects.Count - 1)
        {
            return;
        }
        if (placedIndex != -1)
        {
            var temp = placedStoryObjects[j];
            placedStoryObjects[j] = placedStoryObjects[placedIndex];
            placedStoryObjects[placedIndex] = temp;
        }
        updateOrderButtons(j);
    }

    private void togglePlayMode()
    {
        toggleStoryNodeDetail(false);
        inPlayMode = !inPlayMode;
        var playButtonText = playButton.transform.GetChild(0).GetComponent<Text>();
        if (inPlayMode)
        {
            objectDetailParent.SetActive(false);
            playButtonText.text = "Back";
            for (int i = 1; i < placedStoryObjects.Count; i++)
            {
                placedStoryObjects[i].GetComponent<MeshRenderer>().enabled = false;
            }
        }
        else
        {
            objectDetailParent.SetActive(true);
            playButtonText.text = "Play";
            for (int i = 0; i < placedStoryObjects.Count; i++)
            {
                placedStoryObjects[i].GetComponent<MeshRenderer>().enabled = true;
            }
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

            placedStoryObjects.Add(spawnedObject);

            toggleStoryNodeDetail(true, spawnedObject);

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
        if (selectedObject != null & inPlayMode)
        {
            selectedObject.transform.GetChild(0).transform.LookAt(ARCamera.gameObject.transform);
        }
    }

    private const string PLAY_MODE_OVERHEAD = "Play Mode Overhead(Clone)";

    private void updateOrderButtons(int placedIndex)
    {
        storyOrderText.text = placedIndex.ToString();
        if (placedIndex == 0)
        {
            decreaseOrderButton.gameObject.SetActive(false);
        }
        else
        {
            decreaseOrderButton.gameObject.SetActive(true);
        }
        if (placedIndex >= placedStoryObjects.Count - 1)
        {
            increaseOrderButton.gameObject.SetActive(false);
        }
        else
        {
            increaseOrderButton.gameObject.SetActive(true);
        }
    }

    private void toggleStoryNodeDetail(bool focused, GameObject hitObj = null)
    {
        if (focused && hitObj != null && hitObj.GetComponent<StoryNodeController>() != null)
        {
            var nc = hitObj.GetComponent<StoryNodeController>();
            var placedIndex = placedStoryObjects.IndexOf(hitObj);
            if (inPlayMode)
            {
                if (hitObj.transform.Find(PLAY_MODE_OVERHEAD) == null)
                {
                    var objectDetail = Instantiate(playModeOverheadPrefab, hitObj.transform);
                    // objectDetail.transform.Rotate(0, 180, 0);
                    objectDetail.transform.localPosition = new Vector3(objectDetail.transform.localPosition.x, nc.overheadUIOffset, objectDetail.transform.localPosition.z);
                    objectDetail.transform.GetChild(0).Find("Title").GetComponent<Text>().text = nc.storyNode.title;
                    objectDetail.transform.GetChild(0).Find("Text").GetComponent<Text>().text = nc.storyNode.text;
                    selectedObject = hitObj;
                }
                if (placedIndex == currentPlayModeObjectIndex)
                {
                    currentPlayModeObjectIndex++;
                    placedStoryObjects[currentPlayModeObjectIndex].GetComponent<MeshRenderer>().enabled = true;
                }
            }
            else
            {
                if (placedIndex != -1)
                {
                    updateOrderButtons(placedIndex);
                }
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
                if (selectedObject != null && selectedObject.GetComponent<StoryNodeController>() != null && selectedObject.transform.childCount > 0)
                {
                    Destroy(selectedObject.transform.Find(PLAY_MODE_OVERHEAD).gameObject);
                    selectedObject = null;
                }
            }
            else
            {
                objectTitle.text = "";
                objectDetailParent.SetActive(false);
                selectedObject = null;
                textInput.text = "";
                storyOrderText.text = "";
            }
        }
    }
}
