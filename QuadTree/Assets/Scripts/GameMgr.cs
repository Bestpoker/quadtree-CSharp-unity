using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMgr : MonoBehaviour
{
    public Transform lines;

    public Transform nodes;

    public RectTransform playerTrans;

    private List<QuadTree.QuadTreeRect> allRects = new List<QuadTree.QuadTreeRect>();

    private List<QuadTree.QuadTreeRect> colliderResultRects = new List<QuadTree.QuadTreeRect>(128);

    private Vector2 cameraSize;

    private QuadTree.QuadTreeRect playerRect;

    public QuadTree rootTree;

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

        rootTree = new QuadTree(0, 0, Camera.main.pixelWidth, Camera.main.pixelHeight, maxObjectsInGrid, maxLevel);

        playerRect = new QuadTree.QuadTreeRect
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

        var colliderResultRectsTemp = rootTree.Check(playerRect);

        if (colliderResultRects != null)
        {
            colliderResultRects.AddRange(colliderResultRectsTemp);

            for (var i = 0; i < colliderResultRects.Count; i++)
            {
                colliderResultRects[i].image.color = Color.red;
            }
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

        var rect = new QuadTree.QuadTreeRect
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


public class QuadTree
{
    public class QuadTreeRect
    {
        private static ulong unitIDCounter;
        
        public ulong unitID;
        
        public float height;

        public float width;

        public float x;

        public float y;

        public Image image;
        
        private static Queue<QuadTreeRect> rectPool = new Queue<QuadTreeRect>(256);

        public static QuadTreeRect GetRect()
        {
            unitIDCounter++;

            QuadTreeRect rect = null;
            
            if (rectPool.Count > 0)
            {
                rect = rectPool.Dequeue();
            }
            else
            {
                rect = new QuadTreeRect();
            }

            rect.unitID = unitIDCounter;

            return rect;

        }

        public static QuadTreeRect GetRect(float posX, float posY, float width, float height)
        {
            var rect = GetRect();

            rect.x = posX;

            rect.y = posY;

            rect.width = width;

            rect.height = height;

            return rect;

        }

        public void Recycle()
        {
            if (rectPool.Count < 256)
            {
                rectPool.Enqueue(this);
            }
        }
        
        
        
    }
    
    public QuadTreeRect bounds;

    public List<QuadTree> childTrees;

    public int level;

    public int max_levels;

    public int max_objects;

    public List<QuadTreeRect> rectObs;

    private List<QuadTreeRect> checkObList;

    private HashSet<QuadTreeRect> checkObHash;

    private List<int> indexes;

    public GameObject verticalLine;

    public GameObject horizontalLine;

    public QuadTree(float posX, float posY, float width, float height, int max_objects, int max_levels, int level = 0)
    {
        this.max_objects = max_objects;
        
        this.max_levels = max_levels;

        this.level = level;
        
        this.bounds = QuadTreeRect.GetRect(posX, posY, width, height);

        rectObs = new List<QuadTreeRect>(max_objects);
        
        childTrees = new List<QuadTree>(4);

        var checkCount = max_objects * Mathf.Max(1, max_levels - level);

        checkObList = new List<QuadTreeRect>(checkCount);

        checkObHash = new HashSet<QuadTreeRect>(checkCount);
        
        indexes = new List<int>(4);
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

        verticalLine.GetComponent<RectTransform>().sizeDelta = new Vector2(3, bounds.height);

        verticalLine.transform.localPosition = new Vector3(x, y, 0);

        horizontalLine = Object.Instantiate(prefab, parent);

        horizontalLine.GetComponent<Image>().color = Color.black;

        horizontalLine.GetComponent<RectTransform>().sizeDelta = new Vector2(bounds.width, 3);

        horizontalLine.transform.localPosition = new Vector3(x, y, 0);
        
        childTrees.Add(new QuadTree(x + subWidth / 2, y + subHeight / 2, subWidth, subHeight,max_objects, max_levels, nextLevel));

        childTrees.Add(new QuadTree(x - subWidth / 2, y + subHeight / 2, subWidth, subHeight, max_objects, max_levels, nextLevel));

        childTrees.Add(new QuadTree(x - subWidth / 2, y - subHeight / 2, subWidth, subHeight, max_objects, max_levels, nextLevel));
        
        childTrees.Add(new QuadTree(x + subWidth / 2, y - subHeight / 2, subWidth, subHeight, max_objects, max_levels, nextLevel));
    }


    public List<int> GetIndex(QuadTreeRect pRect)
    {
        indexes.Clear();
        
        var startIsNorth = pRect.y + pRect.height / 2 > bounds.y;
        
        var startIsWest = pRect.x - pRect.width / 2 < bounds.x;
        
        var endIsEast = pRect.x + pRect.width / 2 > bounds.x;
        
        var endIsSouth = pRect.y - pRect.height / 2 < bounds.y;

        if (startIsNorth && endIsEast) indexes.Add(0);

        if (startIsNorth && startIsWest) indexes.Add(1);

        if (startIsWest && endIsSouth) indexes.Add(2);

        if (endIsEast && endIsSouth) indexes.Add(3);

        return indexes;
    }

    public void Insert(QuadTreeRect pRect)
    {
        var i = 0;

        if (childTrees.Count > 0)
        {
            var indexesTemp = GetIndex(pRect);

            for (i = 0; i < indexesTemp.Count; i++)
            {
                var childTreeIndex = indexesTemp[i];

                childTrees[childTreeIndex].Insert(pRect);
            }

            return;
        }

        rectObs.Add(pRect);

        if (rectObs.Count > max_objects && level < max_levels)
        {
            if (childTrees.Count == 0) Split();

            for (i = 0; i < rectObs.Count; i++)
            {
                var indexesTemp = GetIndex(rectObs[i]);

                for (var k = 0; k < indexesTemp.Count; k++)
                {
                    var childTreeIndex = indexesTemp[k];

                    childTrees[childTreeIndex].Insert(rectObs[i]);
                }
            }

            rectObs.Clear();
        }
    }

    public QuadTreeRect Insert(float posX, float posY, float width, float height)
    {
        var pRect = QuadTreeRect.GetRect(posX, posY, width, height);
        
        Insert(pRect);

        return pRect;
    }

    public List<QuadTreeRect> Check(QuadTreeRect pRect)
    {
        checkObHash.Clear();

        checkObList.Clear();

        for (int i = 0; i < rectObs.Count; i++)
        {
            var rectTemp = rectObs[i];

            if (checkObHash.Add(rectTemp))
            {
                checkObList.AddRange(rectObs);
            }
        }
        

        if (childTrees.Count > 0)
        {
            var indexesTemp = GetIndex(pRect);
            
            for (var i = 0; i < indexesTemp.Count; i++)
            {
                var childTreeIndex = indexesTemp[i];
                
                var tempList = childTrees[childTreeIndex].Check(pRect);

                for (int j = 0; j < tempList.Count; j++)
                {
                    var rectTemp = tempList[j];

                    if (checkObHash.Add(rectTemp))
                    {
                        checkObList.Add(rectTemp);
                    }
                    
                }
                
            }
        }

        return checkObList;
    }

    public void Clear()
    {
        for (var i = 0; i < rectObs.Count; i++)
        {
            if (rectObs[i].image != null)
            {
                Object.Destroy(rectObs[i].image.gameObject);
            }

            rectObs[i].Recycle();
        }

        rectObs.Clear();
        
        checkObHash.Clear();
        
        checkObList.Clear();

        indexes.Clear();

        if (horizontalLine != null)
        {
            Object.Destroy(horizontalLine);

            horizontalLine = null;
        }

        if (verticalLine != null)
        {
            Object.Destroy(verticalLine);

            verticalLine = null;
        }

        for (var i = 0; i < childTrees.Count; i++)
        {
            childTrees[i].Clear();
        }

        childTrees.Clear();
    }

    
    


    
    
}