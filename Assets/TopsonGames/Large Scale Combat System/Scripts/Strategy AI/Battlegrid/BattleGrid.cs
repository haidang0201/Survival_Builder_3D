namespace TopsonGames.AI.Grid
{
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class BattleGrid
    {
        public int width;
        public int height;
        public float cellSize;
        public Vector3 originPosition;

        private float[] gridArray;
        private float currentMaxValue = 1f;

        public BattleGrid(int width, int height, float cellSize, Vector3 originPosition)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.originPosition = originPosition;
            this.gridArray = new float[width * height];
        }

        public void Clear()
        {
            if (gridArray != null)
                System.Array.Clear(gridArray, 0, gridArray.Length);
            currentMaxValue = 1f;
        }

        public void SetRawData(float[] rawData)
        {
            this.gridArray = rawData;
            currentMaxValue = 1f;
            for (int i = 0; i < rawData.Length; i++)
            {
                if (rawData[i] > currentMaxValue)
                    currentMaxValue = rawData[i];
            }
        }

        public void AddValue(Vector3 worldPosition, float value, float radius)
        {
            GetXY(worldPosition, out int x, out int y);
            int radiusInCells = Mathf.CeilToInt(radius / cellSize);

            for (int i = x - radiusInCells; i <= x + radiusInCells; i++)
            {
                for (int j = y - radiusInCells; j <= y + radiusInCells; j++)
                {
                    if (IsValid(i, j))
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(i, j));
                        if (dist <= radiusInCells)
                        {
                            float falloff = 1f - (dist / radiusInCells);
                            AddToCell(i, j, value * falloff);
                        }
                    }
                }
            }
        }

        public void AddValueCone(Vector3 origin, Vector3 forwardDir, float value, float range, float angleDegrees)
        {
            GetXY(origin, out int startX, out int startY);
            int radiusInCells = Mathf.CeilToInt(range / cellSize);
            float halfAngle = angleDegrees * 0.5f;
            float angleThreshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

            for (int i = startX - radiusInCells; i <= startX + radiusInCells; i++)
            {
                for (int j = startY - radiusInCells; j <= startY + radiusInCells; j++)
                {
                    if (IsValid(i, j))
                    {
                        Vector3 cellWorldPos = GetWorldPosition(i, j);
                        float dist = Vector3.Distance(origin, cellWorldPos);
                        if (dist > range) continue;

                        Vector3 dirToCell = (cellWorldPos - origin).normalized;
                        float dot = Vector3.Dot(forwardDir, dirToCell);

                        if (dot >= angleThreshold)
                        {
                            float falloff = 1f - (dist / range);
                            AddToCell(i, j, value * falloff);
                        }
                    }
                }
            }
        }

        public void AddValueRotatedBox(Vector3 center, Vector3 size, Quaternion rotation, float value)
        {
            float maxDim = Mathf.Max(size.x, size.z);
            int radiusInCells = Mathf.CeilToInt((maxDim * 1.5f) / cellSize);

            GetXY(center, out int centerX, out int centerY);
            Matrix4x4 worldToLocal = Matrix4x4.TRS(center, rotation, Vector3.one).inverse;
            Vector3 halfSize = size * 0.5f;

            for (int i = centerX - radiusInCells; i <= centerX + radiusInCells; i++)
            {
                for (int j = centerY - radiusInCells; j <= centerY + radiusInCells; j++)
                {
                    if (IsValid(i, j))
                    {
                        Vector3 worldPos = GetWorldPosition(i, j);
                        Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos);

                        if (Mathf.Abs(localPos.x) <= halfSize.x && Mathf.Abs(localPos.z) <= halfSize.z)
                        {
                            AddToCell(i, j, value);
                        }
                    }
                }
            }
        }

        private void AddToCell(int x, int y, float amount)
        {
            int index = GetIndex(x, y);
            gridArray[index] += amount;
            if (gridArray[index] > currentMaxValue)
                currentMaxValue = gridArray[index];
        }

        public float GetValue(Vector3 worldPosition)
        {
            GetXY(worldPosition, out int x, out int y);
            if (IsValid(x, y)) return gridArray[GetIndex(x, y)];
            return 0f;
        }

        public Vector3 GetNearestSafePosition(Vector3 targetPos, float maxRisk, int clearance = 0)
        {
            GetXY(targetPos, out int startX, out int startY);

            if (IsSafe(startX, startY, maxRisk, clearance))
                return GetWorldPosition(startX, startY);

            int maxRadius = 30;
            for (int r = 1; r <= maxRadius; r++)
            {
                for (int x = startX - r; x <= startX + r; x++)
                {
                    for (int y = startY - r; y <= startY + r; y++)
                    {
                        if (x == startX - r || x == startX + r || y == startY - r || y == startY + r)
                        {
                            if (IsSafe(x, y, maxRisk, clearance))
                            {
                                return GetWorldPosition(x, y);
                            }
                        }
                    }
                }
            }
            return targetPos;
        }

        private bool IsSafe(int x, int y, float maxRisk, int clearance)
        {
            if (!IsValid(x, y)) return false;

            float danger = 0f;
            for (int cx = -clearance; cx <= clearance; cx++)
            {
                for (int cy = -clearance; cy <= clearance; cy++)
                {
                    int checkX = x + cx;
                    int checkY = y + cy;

                    if (IsValid(checkX, checkY))
                    {
                        float val = gridArray[GetIndex(checkX, checkY)];
                        if (val > danger) danger = val;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return danger <= maxRisk;
        }

        public List<Vector3> FindSafePath(Vector3 startWorldPos, Vector3 endWorldPos, float maxRiskPerNode = 50f, float ignoreTargetDangerDist = 35f, float ignoreStartDangerDist = 15f, int clearance = 1)
        {
            GetXY(startWorldPos, out int startX, out int startY);
            GetXY(endWorldPos, out int endX, out int endY);

            if (!IsValid(startX, startY)) return null;

            if (ignoreTargetDangerDist <= 0f && (!IsValid(endX, endY) || gridArray[GetIndex(endX, endY)] > maxRiskPerNode))
            {
                bool found = false;
                for (int r = 1; r < 6; r++)
                {
                    for (int x = endX - r; x <= endX + r; x++)
                    {
                        for (int y = endY - r; y <= endY + r; y++)
                        {
                            if (IsValid(x, y) && gridArray[GetIndex(x, y)] <= maxRiskPerNode)
                            {
                                endX = x; endY = y;
                                found = true; break;
                            }
                        }
                        if (found) break;
                    }
                    if (found) break;
                }
                if (!found) return null;
            }

            List<PathNode> openList = new List<PathNode>();
            HashSet<int> closedList = new HashSet<int>();

            PathNode startNode = new PathNode(startX, startY, 0, 0, null);
            openList.Add(startNode);

            int iterations = 0;
            int maxIterations = 5000;

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                int lowestIndex = 0;
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < openList[lowestIndex].FCost)
                        lowestIndex = i;
                }

                PathNode currentNode = openList[lowestIndex];
                openList.RemoveAt(lowestIndex);

                if (currentNode.x == endX && currentNode.y == endY) return RetracePath(currentNode);

                closedList.Add(GetIndex(currentNode.x, currentNode.y));

                for (int i = 0; i < 8; i++)
                {
                    int checkX = currentNode.x + dx[i];
                    int checkY = currentNode.y + dy[i];

                    if (!IsValid(checkX, checkY)) continue;
                    if (closedList.Contains(GetIndex(checkX, checkY))) continue;

                    float distToTarget = GetDistanceFast(checkX, checkY, endX, endY) * cellSize;
                    float distToStart = GetDistanceFast(checkX, checkY, startX, startY) * cellSize;

                    bool nearTarget = ignoreTargetDangerDist > 0f && (distToTarget <= ignoreTargetDangerDist);
                    bool escapingDanger = ignoreStartDangerDist > 0f && (distToStart <= ignoreStartDangerDist);

                    float danger = 0f;
                    bool isTargetNode = (checkX == endX && checkY == endY);

                    if (ignoreTargetDangerDist > 0f && isTargetNode)
                    {
                        danger = 0f;
                    }
                    else
                    {
                        for (int cx = -clearance; cx <= clearance; cx++)
                        {
                            for (int cy = -clearance; cy <= clearance; cy++)
                            {
                                int clearX = checkX + cx;
                                int clearY = checkY + cy;
                                if (IsValid(clearX, clearY))
                                {
                                    float val = gridArray[GetIndex(clearX, clearY)];
                                    if (val > danger) danger = val;
                                }
                            }
                        }
                    }

                    if (danger > maxRiskPerNode && !nearTarget && !escapingDanger) continue;

                    float dangerToApply = (nearTarget || escapingDanger) ? 0f : danger;

                    float linearPenalty = dangerToApply * 150.0f;
                    float exponentialPenalty = (dangerToApply * dangerToApply) * 50.0f;
                    float riskCost = linearPenalty + exponentialPenalty;

                    float newMovementCost = currentNode.gCost + GetDistanceFast(currentNode.x, currentNode.y, checkX, checkY) + riskCost;

                    PathNode existingNode = null;
                    for (int j = 0; j < openList.Count; j++)
                    {
                        if (openList[j].x == checkX && openList[j].y == checkY)
                        {
                            existingNode = openList[j];
                            break;
                        }
                    }

                    if (existingNode == null || newMovementCost < existingNode.gCost)
                    {
                        if (existingNode == null)
                        {
                            float hCost = GetDistanceFast(checkX, checkY, endX, endY);
                            openList.Add(new PathNode(checkX, checkY, newMovementCost, hCost, currentNode));
                        }
                        else
                        {
                            existingNode.gCost = newMovementCost;
                            existingNode.parent = currentNode;
                        }
                    }
                }
            }
            return null;
        }

        private List<Vector3> RetracePath(PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode current = endNode;
            while (current != null)
            {
                path.Add(GetWorldPosition(current.x, current.y));
                current = current.parent;
            }
            path.Reverse();
            return path;
        }

        private void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
            y = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
        }

        private Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x * cellSize, originPosition.y, y * cellSize) + originPosition;
        }

        public Vector3 GetBestPositionInRadius(Vector3 center, float radius, bool findLowest = false)
        {
            GetXY(center, out int cx, out int cy);
            int r = Mathf.CeilToInt(radius / cellSize);
            float bestValue = findLowest ? float.MaxValue : float.MinValue;
            Vector3 bestPos = center;

            for (int i = cx - r; i <= cx + r; i++)
            {
                for (int j = cy - r; j <= cy + r; j++)
                {
                    if (IsValid(i, j))
                    {
                        float val = gridArray[GetIndex(i, j)];
                        bool isBetter = findLowest ? (val < bestValue) : (val > bestValue);
                        if (isBetter) { bestValue = val; bestPos = GetWorldPosition(i, j); }
                    }
                }
            }
            return bestPos;
        }

        private int GetIndex(int x, int y) { return x + y * width; }
        private bool IsValid(int x, int y) { return x >= 0 && y >= 0 && x < width && y < height; }

        private float GetDistanceFast(int x1, int y1, int x2, int y2) { return Mathf.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)); }

        public void DrawGizmos(Color color, float cutoff = 0.1f)
        {
            if (gridArray == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float val = gridArray[GetIndex(x, y)];
                    if (val > cutoff)
                    {
                        float normalizedAlpha = val / currentMaxValue;
                        float alpha = Mathf.Lerp(0.1f, 0.9f, normalizedAlpha);
                        Gizmos.color = new Color(color.r, color.g, color.b, alpha);
                        Vector3 center = GetWorldPosition(x, y) + new Vector3(cellSize * 0.5f, 0.5f, cellSize * 0.5f);
                        Gizmos.DrawCube(center, new Vector3(cellSize, 0.2f, cellSize) * 0.9f);
                    }
                }
            }
        }

        private class PathNode
        {
            public int x, y;
            public float gCost, hCost;
            public float FCost => gCost + hCost;
            public PathNode parent;
            public PathNode(int x, int y, float g, float h, PathNode p) { this.x = x; this.y = y; this.gCost = g; this.hCost = h; this.parent = p; }
        }
    }
}