using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    private Stack<GameObject> obstaclePool = new Stack<GameObject>();
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
        Obstacle obstacleData = data as Obstacle;
        GameObject obstacle = GetObstacle();

        float scale = obstacleData.cellSize / obstacle.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        obstacle.transform.localScale = new Vector3(scale, scale, 1);
        obstacle.transform.localPosition = new Vector2(obstacleData.posX, obstacleData.posY);
    }



    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.SpawnObstacle, SpawnObstacle);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.SpawnObstacle, SpawnObstacle);
    }
}
[System.Serializable]
public class Obstacle
{
    public float cellSize;
    public float posX;
    public float posY;
}
