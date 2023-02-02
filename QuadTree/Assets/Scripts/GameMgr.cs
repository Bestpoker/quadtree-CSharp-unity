using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMgr : MonoBehaviour
{
    public Transform lines;

    public Transform nodes;

    public RectTransform playerTrans;

    private List<Quadtree.Rect> allRects = new List<Quadtree.Rect>();

    private List<Quadtree.Rect> colliderResultRects;

    private Vector2 cameraSize;

    private Quadtree.Rect playerRect;

    public Quadtree rootTree;

    public static GameObject imagePrefab;

    public static Transform lineParent;

    public int maxObjectsInGrid = 4;

    public int maxLevel = 4;


    // Start is called before the first frame update
    private void Start()
    {
        imagePrefab = playerTrans.gameObject;

        lineParent = lines;

        cameraSize.x = Camera.main.pixelWidth / 2f;
        cameraSize.y = Camera.main.pixelHeight / 2f;

        rootTree = new Quadtree(new Quadtree.Rect
        {
            x = 0,
            y = 0,
            width = Camera.main.pixelWidth,
            height = Camera.main.pixelHeight
        }, maxObjectsInGrid, maxLevel);

        playerRect = new Quadtree.Rect
        {
            width = playerTrans.sizeDelta.x,
            height = playerTrans.sizeDelta.y
        };
    }

    // Update is called once per frame
    private void Update()
    {
        playerTrans.position = Input.mousePosition;

        playerRect.x = playerTrans.localPosition.x;

        playerRect.y = playerTrans.localPosition.y;

        if (colliderResultRects != null && colliderResultRects.Count > 0)
        {
            for (var i = 0; i < colliderResultRects.Count; i++)
            {
                colliderResultRects[i].image.color = Color.green;
            }

            colliderResultRects.Clear();
        }

        if (Input.GetKeyUp(KeyCode.A)) AddCube();

        if (Input.GetKeyUp(KeyCode.C)) Clear();

        colliderResultRects = rootTree.Retrieve(playerRect);

        for (var i = 0; i < colliderResultRects.Count; i++)
        {
            colliderResultRects[i].image.color = Color.red;
        }
    }

    public void AddCube()
    {
        var x = Random.Range(-cameraSize.x, cameraSize.x);

        var y = Random.Range(-cameraSize.y, cameraSize.y);

        var width = Random.Range(2, cameraSize.x / 3);

        var height = Random.Range(2, cameraSize.y / 3);

        var rt = Instantiate(playerTrans.gameObject, nodes).GetComponent<RectTransform>();

        rt.sizeDelta = new Vector2(width, height);

        rt.localPosition = new Vector3(x, y, 0);

        var image = rt.GetComponent<Image>();
        image.color = Color.green;

        var rect = new Quadtree.Rect
        {
            x = x,
            y = y,
            width = width,
            height = height,
            image = image
        };

        allRects.Add(rect);

        rootTree.Insert(rect);
    }

    public void Clear()
    {
        colliderResultRects?.Clear();

        allRects?.Clear();

        rootTree.Clear();
    }
}

public class Quadtree
{
    public Rect bounds;

    public List<Quadtree> childTrees;

    public int level;

    public int max_levels;

    public int max_objects;

    public List<Rect> rectObs;

    public GameObject verticalLine;

    public GameObject horizontalLine;


    public Quadtree(Rect bounds, int max_objects, int max_levels, int level = 0)
    {
        this.max_objects = max_objects;
        this.max_levels = max_levels;

        this.level = level;
        this.bounds = bounds;

        rectObs = new List<Rect>();
        childTrees = new List<Quadtree>(4);
    }

    public void Split()
    {
        var nextLevel = level + 1;
        var subWidth = bounds.width / 2;
        var subHeight = bounds.height / 2;
        var x = bounds.x;
        var y = bounds.y;

        var parent = GameMgr.lineParent;

        var prefab = GameMgr.imagePrefab;

        verticalLine = Object.Instantiate(prefab, parent);

        verticalLine.GetComponent<Image>().color = Color.black;

        verticalLine.GetComponent<RectTransform>().sizeDelta = new Vector2(1, bounds.height);

        verticalLine.transform.localPosition = new Vector3(x, y, 0);

        horizontalLine = Object.Instantiate(prefab, parent);

        horizontalLine.GetComponent<Image>().color = Color.black;

        horizontalLine.GetComponent<RectTransform>().sizeDelta = new Vector2(bounds.width, 1);

        horizontalLine.transform.localPosition = new Vector3(x, y, 0);


        //top right node
        childTrees.Add(new Quadtree(new Rect
        {
            x = x + subWidth / 2,
            y = y + subHeight / 2,
            width = subWidth,
            height = subHeight
        }, max_objects, max_levels, nextLevel));

        //top left node
        childTrees.Add(new Quadtree(new Rect
        {
            x = x - subWidth / 2,
            y = y + subHeight / 2,
            width = subWidth,
            height = subHeight
        }, max_objects, max_levels, nextLevel));

        //bottom left node
        childTrees.Add(new Quadtree(new Rect
        {
            x = x - subWidth / 2,
            y = y - subHeight / 2,
            width = subWidth,
            height = subHeight
        }, max_objects, max_levels, nextLevel));

        //bottom right node
        childTrees.Add(new Quadtree(new Rect
        {
            x = x + subWidth / 2,
            y = y - subHeight / 2,
            width = subWidth,
            height = subHeight
        }, max_objects, max_levels, nextLevel));
    }


    public List<int> GetIndex(Rect pRect)
    {
        var indexes = new List<int>();

        var startIsNorth = pRect.y + pRect.height / 2 > bounds.y;
        var startIsWest = pRect.x - pRect.width / 2 < bounds.x;
        var endIsEast = pRect.x + pRect.width / 2 > bounds.x;
        var endIsSouth = pRect.y - pRect.height / 2 < bounds.y;

        //top-right quad
        if (startIsNorth && endIsEast) indexes.Add(0);

        //top-left quad
        if (startIsNorth && startIsWest) indexes.Add(1);

        //bottom-left quad
        if (startIsWest && endIsSouth) indexes.Add(2);

        //bottom-right quad
        if (endIsEast && endIsSouth) indexes.Add(3);

        return indexes;
    }

    public void Insert(Rect pRect)
    {
        var i = 0;

        //if we have subnodes, call insert on matching subnodes
        if (childTrees.Count > 0)
        {
            var indexes = GetIndex(pRect);

            for (i = 0; i < indexes.Count; i++) childTrees[indexes[i]].Insert(pRect);
            return;
        }

        //otherwise, store object here
        rectObs.Add(pRect);

        //max_objects reached
        if (rectObs.Count > max_objects && level < max_levels)
        {
            //split if we don't already have subnodes
            if (childTrees.Count == 0) Split();

            //add all objects to their corresponding subnode
            for (i = 0; i < rectObs.Count; i++)
            {
                var indexes = GetIndex(rectObs[i]);
                for (var k = 0; k < indexes.Count; k++) childTrees[indexes[k]].Insert(rectObs[i]);
            }

            //clean up this node
            rectObs.Clear();
        }
    }

    public List<Rect> Retrieve(Rect pRect)
    {
        var indexes = GetIndex(pRect);
        var returnObjects = new List<Rect>();

        returnObjects.AddRange(rectObs);

        //if we have subnodes, retrieve their objects
        if (childTrees.Count > 0)
            for (var i = 0; i < indexes.Count; i++)
            {
                var temp = childTrees[indexes[i]].Retrieve(pRect);

                returnObjects.AddRange(temp);
            }


        //remove duplicates
        for (var i = returnObjects.Count - 1; i >= 0; i--)
        {
            var item = returnObjects[i];
            var firstIndex = returnObjects.IndexOf(item);

            if (firstIndex != i) returnObjects.RemoveAt(i);
        }

        return returnObjects;
    }

    public void Clear()
    {
        for (var i = 0; i < rectObs.Count; i++)
        {
            if (rectObs[i].image != null)
            {
                GameObject.Destroy(rectObs[i].image.gameObject);
            }
        }

        rectObs.Clear();

        if (horizontalLine != null)
        {
            GameObject.Destroy(horizontalLine);
        }

        if (verticalLine != null)
        {
            GameObject.Destroy(verticalLine);
        }

        for (var i = 0; i < childTrees.Count; i++)
            childTrees[i].Clear();


        childTrees.Clear();
    }


    public class Rect
    {
        public float height;

        public float width;

        public float x;

        public float y;

        public Image image;
    }
}