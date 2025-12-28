using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    private Stack<GameObject> obstaclePool = new Stack<GameObject>();
    [HideInInspector] public Dictionary<Vector2, GameObject> activeObstacles = new Dictionary<Vector2, GameObject>();
    [SerializeField] private Sprite obstacleSprite;
    [SerializeField] private Transform obstacleParent;

    [HideInInspector] public float obstacleSize;
    private void Start()
    {
        for (int i = 0; i < 20; i++)
        {
            InitObstacle();
        }
    }
    private void InitObstacle()
    {
        GameObject obstacle = new GameObject("obstacle");
        obstacle.transform.parent = obstacleParent;
        SpriteRenderer renderer = obstacle.AddComponent<SpriteRenderer>();
        renderer.sprite = obstacleSprite;
        obstacle.SetActive(false);
        obstaclePool.Push(obstacle);
    }
    private GameObject GetObstacle()
    {
        if (obstaclePool.Count == 0)
            InitObstacle();
        GameObject go = obstaclePool.Pop();
        go.SetActive(true);
        return go;
    }
    public void SpawnObstacle(object data)
    {
        var obstacleInfoList = data as List<Obstacle>;
        obstacleSize = obstacleInfoList[0].cellSize;
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            foreach (Obstacle item in obstacleInfoList)
            {
                GameObject obstacle = GetObstacle();

                float scale = item.cellSize / obstacle.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
                obstacle.transform.localScale = new Vector3(scale, scale, 1);
                obstacle.transform.localPosition = new Vector2(item.posX, item.posY);

                Vector2 key = new Vector2(Mathf.Round(item.posX * 1000f) / 1000f, Mathf.Round(item.posY * 1000f) / 1000f);
                activeObstacles[key] = obstacle;
            }
        });

    }

    private void RemoveObstacle(object data)
    {
        var obstacleData = data as Obstacle;
        if (obstacleData == null)
            return;
        GameObject obstacle;
        Vector2 key = new Vector2(Mathf.Round(obstacleData.posX * 1000f) / 1000f, Mathf.Round(obstacleData.posY * 1000f) / 1000f);
        if (activeObstacles.TryGetValue(key, out obstacle))
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                obstacle.SetActive(false);
                activeObstacles.Remove(key);
            });
        }
    }
    public void RemoveAllObstacles()
    {
        foreach(GameObject item in activeObstacles.Values)
        {
            item.SetActive(false);  
        }
    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.SpawnObstacle, SpawnObstacle);
        EventSystem.Subscribe(MessageType.RemoveObstacle, RemoveObstacle);

    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.SpawnObstacle, SpawnObstacle);
        EventSystem.Unsubscribe(MessageType.RemoveObstacle, RemoveObstacle);
    }
}
[System.Serializable]
public class Obstacle
{
    public float cellSize;
    public float posX;
    public float posY;
}
