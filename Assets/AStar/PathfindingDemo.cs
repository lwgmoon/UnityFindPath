using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

namespace AStar
{
    /// <summary>
    /// A星寻路演示脚本，用于展示如何使用A星寻路系统
    /// </summary>
    public class PathfindingDemo : MonoBehaviour
    {
        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private GameObject pathFollower;
        [SerializeField] private float moveSpeed = 2f;
        
        [Header("交互设置")]
        [SerializeField] private KeyCode setStartKey = KeyCode.S;
        [SerializeField] private KeyCode setEndKey = KeyCode.E;
        [SerializeField] private KeyCode toggleWalkableKey = KeyCode.Space;
        [SerializeField] private KeyCode resetGridKey = KeyCode.R;
        
        private Camera mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main;
            
            if (gridVisualizer == null)
            {
                gridVisualizer = FindObjectOfType<GridVisualizer>();
            }
            
            // 显示使用说明
            Debug.Log("A*寻路演示：\n" +
                      $"按住 {setStartKey} 并点击设置起点\n" +
                      $"按住 {setEndKey} 并点击设置终点\n" +
                      $"按住 {toggleWalkableKey} 并点击切换节点是否可通行\n" +
                      $"按 {resetGridKey} 重置网格");
        }
        
        private void Update()
        {
            // 处理鼠标输入
            if (Input.GetMouseButton(0))
            {
                // 将鼠标位置转换为射线
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                // 射线检测网格节点
                if (Physics.Raycast(ray, out hit))
                {
                    // 获取点击的节点坐标
                    GameObject nodeObj = hit.collider.gameObject;
                    if (nodeObj.transform.parent == gridVisualizer.transform)
                    {
                        string[] nameParts = nodeObj.name.Split('_');
                        if (nameParts.Length >= 3)
                        {
                            int x = int.Parse(nameParts[1]);
                            int y = int.Parse(nameParts[2]);
                            
                            // 根据按键执行不同操作
                            if (Input.GetKey(setStartKey))
                            {
                                // 设置起点
                                gridVisualizer.SetStartPosition(x, y);
                            }
                            else if (Input.GetKey(setEndKey))
                            {
                                // 设置终点
                                gridVisualizer.SetEndPosition(x, y);
                            }
                            else if (Input.GetKey(toggleWalkableKey))
                            {
                                // 切换节点是否可通行
                                Node node = gridVisualizer.Pathfinding.GetNode(x, y);
                                if (node != null)
                                {
                                    if (node.Walkable)
                                        gridVisualizer.SetNodeUnwalkable(x, y);
                                    else
                                        gridVisualizer.SetNodeWalkable(x, y);
                                }
                            }
                        }
                    }
                }
            }
            
            // 重置网格
            if (Input.GetKeyDown(resetGridKey))
            {
                gridVisualizer.ResetGrid();
            }
        }


        private void OnGUI()
        {
            if (GUILayout.Button("使用A*算法示例"))
                AStarExample();

            if (GUILayout.Button("开始寻路"))
            {
                StartPathFollowing();
            }
        }

        /// <summary>
        /// 使用A*算法示例
        /// </summary>
        public void AStarExample()
        {
            // 创建一个10x10的网格
            AStarPathfinding pathfinding = new AStarPathfinding(10, 10);
            
            // 设置一些障碍物
            List<Vector2Int> obstacles = new List<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(2, 4),
                new Vector2Int(2, 5),
                new Vector2Int(5, 5),
                new Vector2Int(6, 5),
                new Vector2Int(7, 5)
            };
            
            // 重新创建网格，包含障碍物
            pathfinding = new AStarPathfinding(10, 10, obstacles);
            
            // 寻找从(0,0)到(9,9)的路径
            List<Vector2Int> path = pathfinding.FindPath(0, 0, 9, 9);
            
            // 输出路径
            if (path.Count > 0)
            {
                Debug.Log("找到路径！路径点数量: " + path.Count);
                foreach (var point in path)
                {
                    Debug.Log($"路径点: ({point.x}, {point.y})");
                }
            }
            else
            {
                Debug.Log("未找到路径！");
            }
        }

        /// <summary>
        /// 开始路径跟随
        /// </summary>
        public void StartPathFollowing()
        {
            if (gridVisualizer == null || pathFollower == null)
            {
                Debug.LogError("GridVisualizer 或 PathFollower 未设置!");
                return;
            }

            List<Vector2Int> path = gridVisualizer.GetCurrentPath();

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("未找到路径或路径为空!");
                return;
            }

            StartCoroutine(FollowPathCoroutine(path));
        }

        /// <summary>
        /// 跟随路径的协程
        /// </summary>
        private IEnumerator FollowPathCoroutine(List<Vector2Int> path)
        {
            if (pathFollower == null) yield break;

            Debug.Log("开始跟随路径...");

            foreach (var point in path)
            {
                // 将网格坐标转换为世界坐标
                // 假设 GridVisualizer 的 cellSize 属性可用并且代表单元格大小
                // 并且网格的 (0,0) 点对应世界坐标的 (0,0,0) 加上 GridVisualizer 的偏移
                // 这里简化处理，直接使用 cellSize 进行缩放，并假设 Y 轴为高度
                Vector3 targetPosition = new Vector3(point.x * gridVisualizer.CellSize, 
                                                     pathFollower.transform.position.y, // 保持当前Y轴高度
                                                     point.y * gridVisualizer.CellSize) + gridVisualizer.transform.position;

                while (Vector3.Distance(pathFollower.transform.position, targetPosition) > 0.01f)
                {
                    pathFollower.transform.position = Vector3.MoveTowards(pathFollower.transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    yield return null; // 等待下一帧
                }
                // 确保精确到达目标点
                pathFollower.transform.position = targetPosition;
            }

            Debug.Log("路径跟随完毕!");
        }
    }
}