using System.Collections.Generic;
using UnityEngine;

namespace AStar
{
    /// <summary>
    /// 网格节点类，表示寻路网格中的一个单元格
    /// </summary>
    public class Node
    {   
        // 节点在网格中的坐标
        public int X { get; private set; }
        public int Y { get; private set; }
        
        // 节点是否可通行
        public bool Walkable { get; set; }
        
        // 父节点，用于回溯路径
        public Node Parent { get; set; }
        
        // G值：从起点到当前节点的实际代价
        public float GCost { get; set; }
        // H值：从当前节点到终点的估计代价（启发式函数）
        public float HCost { get; set; }
        // F值：G值 + H值，用于A*算法的评估函数
        public float FCost => GCost + HCost;
        
        public Node(int x, int y, bool walkable)
        {
            X = x;
            Y = y;
            Walkable = walkable;
        }
    }
    
    /// <summary>
    /// A*寻路算法实现类
    /// </summary>
    public class AStarPathfinding
    {
        private Node[,] grid; // 寻路网格
        private int width, height; // 网格尺寸
        
        // 方向数组，用于查找相邻节点（上、右、下、左、左上、右上、右下、左下）
        private static readonly int[] dx = { 0, 1, 0, -1, -1, 1, 1, -1 };
        private static readonly int[] dy = { -1, 0, 1, 0, -1, -1, 1, 1 };
        
        /// <summary>
        /// 初始化寻路网格
        /// </summary>
        /// <param name="width">网格宽度</param>
        /// <param name="height">网格高度</param>
        /// <param name="unwalkablePositions">不可通行的位置列表</param>
        public AStarPathfinding(int width, int height, List<Vector2Int> unwalkablePositions = null)
        {
            this.width = width;
            this.height = height;
            CreateGrid(unwalkablePositions);
        }
        
        /// <summary>
        /// 创建寻路网格
        /// </summary>
        private void CreateGrid(List<Vector2Int> unwalkablePositions)
        {
            grid = new Node[width, height];
            
            // 初始化所有节点为可通行
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = new Node(x, y, true);
                }
            }
            
            // 设置不可通行的节点
            if (unwalkablePositions != null)
            {
                foreach (var pos in unwalkablePositions)
                {
                    if (IsValidPosition(pos.x, pos.y))
                    {
                        grid[pos.x, pos.y].Walkable = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查坐标是否在网格范围内
        /// </summary>
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        
        /// <summary>
        /// 获取指定位置的节点
        /// </summary>
        public Node GetNode(int x, int y)
        {
            if (IsValidPosition(x, y))
                return grid[x, y];
            return null;
        }
        
        /// <summary>
        /// 获取节点的相邻节点列表
        /// </summary>
        private List<Node> GetNeighbors(Node node, bool allowDiagonal = true)
        {
            List<Node> neighbors = new List<Node>();
            int limit = allowDiagonal ? 8 : 4; // 是否允许对角线移动
            
            for (int i = 0; i < limit; i++)
            {
                int newX = node.X + dx[i];
                int newY = node.Y + dy[i];
                
                if (IsValidPosition(newX, newY))
                {
                    neighbors.Add(grid[newX, newY]);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// 计算两点间的曼哈顿距离（启发式函数）
        /// </summary>
        private float CalculateHCost(Node a, Node b)
        {
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }
        
        /// <summary>
        /// 使用A*算法寻找从起点到终点的路径
        /// </summary>
        /// <param name="startX">起点X坐标</param>
        /// <param name="startY">起点Y坐标</param>
        /// <param name="endX">终点X坐标</param>
        /// <param name="endY">终点Y坐标</param>
        /// <param name="allowDiagonal">是否允许对角线移动</param>
        /// <returns>路径点列表，如果没有找到路径则返回空列表</returns>
        public List<Vector2Int> FindPath(int startX, int startY, int endX, int endY, bool allowDiagonal = true)
        {
            // 检查起点和终点是否有效
            if (!IsValidPosition(startX, startY) || !IsValidPosition(endX, endY))
                return new List<Vector2Int>();
                
            Node startNode = grid[startX, startY];
            Node endNode = grid[endX, endY];
            
            // 如果起点或终点不可通行，返回空路径
            if (!startNode.Walkable || !endNode.Walkable)
                return new List<Vector2Int>();
                
            // 开放列表（待评估的节点）
            List<Node> openSet = new List<Node>();
            // 关闭列表（已评估的节点）
            HashSet<Node> closedSet = new HashSet<Node>();
            
            // 将起点加入开放列表
            openSet.Add(startNode);
            
            // 当开放列表不为空时循环
            while (openSet.Count > 0)
            {
                // 找到开放列表中F值最小的节点
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost || 
                        (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                    {
                        currentNode = openSet[i];
                    }
                }
                
                // 将当前节点从开放列表移到关闭列表
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                
                // 如果当前节点是终点，构建并返回路径
                if (currentNode == endNode)
                {
                    return RetracePath(startNode, endNode);
                }
                
                // 检查所有相邻节点
                foreach (var neighbor in GetNeighbors(currentNode, allowDiagonal))
                {
                    // 跳过不可通行或已在关闭列表中的节点
                    if (!neighbor.Walkable || closedSet.Contains(neighbor))
                        continue;
                        
                    // 计算从起点经过当前节点到相邻节点的代价
                    float newGCost = currentNode.GCost + 
                        (neighbor.X != currentNode.X && neighbor.Y != currentNode.Y ? 1.414f : 1f); // 对角线移动代价为√2
                    
                    // 如果新路径更好或节点不在开放列表中
                    if (newGCost < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        // 更新节点信息
                        neighbor.GCost = newGCost;
                        neighbor.HCost = CalculateHCost(neighbor, endNode);
                        neighbor.Parent = currentNode;
                        
                        // 如果节点不在开放列表中，将其加入
                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }
            
            // 没有找到路径，返回空列表
            return new List<Vector2Int>();
        }
        
        /// <summary>
        /// 回溯路径，从终点到起点
        /// </summary>
        private List<Vector2Int> RetracePath(Node startNode, Node endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node currentNode = endNode;
            
            // 从终点回溯到起点
            while (currentNode != startNode)
            {
                path.Add(new Vector2Int(currentNode.X, currentNode.Y));
                currentNode = currentNode.Parent;
            }
            
            // 添加起点
            path.Add(new Vector2Int(startNode.X, startNode.Y));
            
            // 反转路径，使其从起点到终点
            path.Reverse();
            
            return path;
        }
    }
}