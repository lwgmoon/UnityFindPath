using System.Collections.Generic;
using UnityEngine;
using AStar;

namespace AStar
{
    /// <summary>
    /// 网格可视化组件，用于在Unity场景中显示寻路网格和路径
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        [Header("网格设置")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 20;
        [SerializeField] private float cellSize = 1f;
        public float CellSize => cellSize;
        [SerializeField] private bool allowDiagonalMovement = true;
        
        [Header("可视化设置")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Color walkableColor = Color.white;
        [SerializeField] private Color unwalkableColor = Color.red;
        [SerializeField] private Color pathColor = Color.green;
        [SerializeField] private Color startNodeColor = Color.blue;
        [SerializeField] private Color endNodeColor = Color.yellow;
        
        // 寻路算法实例
        private AStarPathfinding pathfinding;
        public AStarPathfinding Pathfinding => pathfinding;
        // 网格节点的游戏对象
        private GameObject[,] nodeObjects;
        // 不可通行的位置列表
        private List<Vector2Int> unwalkablePositions = new List<Vector2Int>();
        // 当前路径
        private List<Vector2Int> currentPath = new List<Vector2Int>();
        public List<Vector2Int> GetCurrentPath() => currentPath;
        // 起点和终点
        private Vector2Int? startPosition = null;
        private Vector2Int? endPosition = null;
        
        private void Start()
        {
            InitializeGrid();
        }
        
        /// <summary>
        /// 初始化网格
        /// </summary>
        private void InitializeGrid()
        {
            // 创建A*寻路实例
            pathfinding = new AStarPathfinding(gridWidth, gridHeight, unwalkablePositions);
            nodeObjects = new GameObject[gridWidth, gridHeight];
            
            // 创建网格可视化
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // 计算节点位置
                    Vector3 position = new Vector3(x * cellSize, 0, y * cellSize);
                    
                    // 创建节点游戏对象
                    GameObject nodeObj = Instantiate(nodePrefab, position, Quaternion.identity, transform);
                    nodeObj.name = $"Node_{x}_{y}";
                    nodeObj.transform.localScale = new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f);
                    
                    // 存储节点引用
                    nodeObjects[x, y] = nodeObj;
                    
                    // 设置节点颜色
                    UpdateNodeColor(x, y);
                }
            }
        }
        
        /// <summary>
        /// 更新节点颜色
        /// </summary>
        private void UpdateNodeColor(int x, int y)
        {
            if (nodeObjects[x, y] == null) return;
            
            Renderer renderer = nodeObjects[x, y].GetComponent<Renderer>();
            if (renderer == null) return;
            
            // 获取节点
            Node node = pathfinding.GetNode(x, y);
            
            // 设置颜色
            if (startPosition.HasValue && startPosition.Value.x == x && startPosition.Value.y == y)
            {
                renderer.material.color = startNodeColor;
            }
            else if (endPosition.HasValue && endPosition.Value.x == x && endPosition.Value.y == y)
            {
                renderer.material.color = endNodeColor;
            }
            else if (IsInPath(x, y))
            {
                renderer.material.color = pathColor;
            }
            else
            {
                renderer.material.color = node.Walkable ? walkableColor : unwalkableColor;
            }
        }
        
        /// <summary>
        /// 检查坐标是否在当前路径中
        /// </summary>
        private bool IsInPath(int x, int y)
        {
            return currentPath.Contains(new Vector2Int(x, y));
        }
        
        /// <summary>
        /// 更新所有节点的颜色
        /// </summary>
        private void UpdateAllNodeColors()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    UpdateNodeColor(x, y);
                }
            }
        }
        
        /// <summary>
        /// 设置节点为不可通行
        /// </summary>
        public void SetNodeUnwalkable(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            
            Node node = pathfinding.GetNode(x, y);
            if (node != null)
            {
                node.Walkable = false;
                
                // 更新不可通行位置列表
                Vector2Int pos = new Vector2Int(x, y);
                if (!unwalkablePositions.Contains(pos))
                {
                    unwalkablePositions.Add(pos);
                }
                
                // 更新节点颜色
                UpdateNodeColor(x, y);
                
                // 如果有起点和终点，重新计算路径
                if (startPosition.HasValue && endPosition.HasValue)
                {
                    FindPath();
                }
            }
        }
        
        /// <summary>
        /// 设置节点为可通行
        /// </summary>
        public void SetNodeWalkable(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            
            Node node = pathfinding.GetNode(x, y);
            if (node != null)
            {
                node.Walkable = true;
                
                // 更新不可通行位置列表
                Vector2Int pos = new Vector2Int(x, y);
                unwalkablePositions.Remove(pos);
                
                // 更新节点颜色
                UpdateNodeColor(x, y);
                
                // 如果有起点和终点，重新计算路径
                if (startPosition.HasValue && endPosition.HasValue)
                {
                    FindPath();
                }
            }
        }
        
        /// <summary>
        /// 设置起点
        /// </summary>
        public void SetStartPosition(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            
            // 清除旧起点的颜色
            if (startPosition.HasValue)
            {
                UpdateNodeColor(startPosition.Value.x, startPosition.Value.y);
            }
            
            // 设置新起点
            startPosition = new Vector2Int(x, y);
            UpdateNodeColor(x, y);
            
            // 如果有终点，计算路径
            if (endPosition.HasValue)
            {
                FindPath();
            }
        }
        
        /// <summary>
        /// 设置终点
        /// </summary>
        public void SetEndPosition(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            
            // 清除旧终点的颜色
            if (endPosition.HasValue)
            {
                UpdateNodeColor(endPosition.Value.x, endPosition.Value.y);
            }
            
            // 设置新终点
            endPosition = new Vector2Int(x, y);
            UpdateNodeColor(x, y);
            
            // 如果有起点，计算路径
            if (startPosition.HasValue)
            {
                FindPath();
            }
        }
        
        /// <summary>
        /// 寻找路径
        /// </summary>
        private void FindPath()
        {
            if (!startPosition.HasValue || !endPosition.HasValue) return;
            
            // 清除旧路径
            ClearPath();
            
            // 计算新路径
            currentPath = pathfinding.FindPath(
                startPosition.Value.x, startPosition.Value.y,
                endPosition.Value.x, endPosition.Value.y,
                allowDiagonalMovement);
            
            // 更新路径节点颜色
            foreach (var pos in currentPath)
            {
                // 跳过起点和终点
                if ((startPosition.HasValue && pos == startPosition.Value) ||
                    (endPosition.HasValue && pos == endPosition.Value))
                    continue;
                    
                UpdateNodeColor(pos.x, pos.y);
            }
        }
        
        /// <summary>
        /// 清除路径
        /// </summary>
        public void ClearPath()
        {
            // 重置路径节点的颜色
            foreach (var pos in currentPath)
            {
                // 跳过起点和终点
                if ((startPosition.HasValue && pos == startPosition.Value) ||
                    (endPosition.HasValue && pos == endPosition.Value))
                    continue;
                    
                UpdateNodeColor(pos.x, pos.y);
            }
            
            // 清空路径列表
            currentPath.Clear();
        }
        
        /// <summary>
        /// 重置网格
        /// </summary>
        public void ResetGrid()
        {
            // 清除路径
            ClearPath();
            
            // 清除起点和终点
            startPosition = null;
            endPosition = null;
            
            // 重置所有节点为可通行
            unwalkablePositions.Clear();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Node node = pathfinding.GetNode(x, y);
                    if (node != null)
                    {
                        node.Walkable = true;
                    }
                }
            }
            
            // 重新初始化网格
            pathfinding = new AStarPathfinding(gridWidth, gridHeight, unwalkablePositions);
            
            // 更新所有节点颜色
            UpdateAllNodeColors();
        }
    }
}