using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    private Stack<GameObject> obstaclePool = new Stack<GameObject>();
    private Dictionary<Vector2, GameObject> activeObstacles = new Dictionary<Vector2, GameObject>();
    [SerializeField] private Sprite obstacleSprite;
    [SerializeField] private Transform obstacleParent;
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
        UnityMainThreadDispatcher.Instance().Enqueue(() =>{
            foreach (Obstacle item in obstacleInfoList)
            {
                GameObject obstacle = GetObstacle();

                float scale = item.cellSize / obstacle.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
                obstacle.transform.localScale = new Vector3(scale, scale, 1);
                obstacle.transform.localPosition = new Vector2(item.posX, item.posY);

                activeObstacles[obstacle.transform.localPosition] = obstacle;
            }
        });

    }

    private void RemoveObstacle(object data)
    {
        var obstacleData = data as Obstacle;
        if (obstacleData == null)
            return;
        GameObject obstacle;
        if (activeObstacles.TryGetValue(new Vector2(obstacleData.posX, obstacleData.posY), out obstacle))
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { 
                obstacle.SetActive(false);
            });
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
