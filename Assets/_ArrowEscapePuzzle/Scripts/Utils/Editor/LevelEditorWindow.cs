#if UNITY_EDITOR
using System.Collections.Generic;
using ArrowGame.Data;
using ArrowGame.Data.LevelProvider;
using ShareCore.Data;
using UnityEditor;
using UnityEngine;

namespace ArrowGame.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelDataSO currentLevel;
    
        private class EditorCell {
            public CellType type = CellType.None;
            public string id = ""; 
        }
        private EditorCell[,] tempGrid;

        // FIX 1: Đổi cọ mặc định sang Đầu Mũi Tên để tránh tô nhầm EmptyDot
        private CellType brushType = CellType.ArrowHeadUp; 
    
        private Vector2 scrollPosition;     
        private float cellSize = 55f; 

        [MenuItem("Tools/Arrow Level Editor (Logic Fixed)")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Arrow Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("🛠 BẢNG VẼ MAP (LOGIC FIXED)", EditorStyles.boldLabel);
        
            currentLevel = (LevelDataSO)EditorGUILayout.ObjectField("Data SO:", currentLevel, typeof(LevelDataSO), false);
            if (currentLevel == null)
            {
                EditorGUILayout.HelpBox("Kéo file LevelDataSO vào đây", MessageType.Warning);
                return;
            }

            // --- 1. KHUNG CÀI ĐẶT MAP ---
            GUILayout.BeginVertical("box");
            currentLevel.levelID = EditorGUILayout.TextField("Level ID:", currentLevel.levelID);

            EditorGUI.BeginChangeCheck();
            int newWidth = EditorGUILayout.IntField("Width (Ngang)", currentLevel.width);
            int newHeight = EditorGUILayout.IntField("Height (Dọc)", currentLevel.height);
        
            if (EditorGUI.EndChangeCheck() || tempGrid == null || tempGrid.GetLength(0) != newWidth || tempGrid.GetLength(1) != newHeight)
            {
                currentLevel.width = Mathf.Max(1, newWidth);
                currentLevel.height = Mathf.Max(1, newHeight);
                LoadDataToGrid();
            }
            GUILayout.EndVertical();

            // --- 2. BẢNG CỌ VẼ (PALETTE) ---
            GUILayout.BeginVertical("box");
            GUILayout.Label("🎨 BẢNG CỌ VẼ:", EditorStyles.boldLabel);
        
            GUILayout.BeginHorizontal();
            DrawBrushButton("↑ Đầu Lên", CellType.ArrowHeadUp);
            DrawBrushButton("↓ Đầu Xuống", CellType.ArrowHeadDown);
            DrawBrushButton("← Đầu Trái", CellType.ArrowHeadLeft);
            DrawBrushButton("→ Đầu Phải", CellType.ArrowHeadRight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBrushButton("╨ Đuôi Lên", CellType.ArrowTailUp);
            DrawBrushButton("╥ Đuôi Xuống", CellType.ArrowTailDown);
            DrawBrushButton("╡ Đuôi Trái", CellType.ArrowTailLeft);
            DrawBrushButton("╞ Đuôi Phải", CellType.ArrowTailRight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBrushButton("═ Thân Ngang", CellType.ArrowBodyHorizontal);
            DrawBrushButton("║ Thân Dọc", CellType.ArrowBodyVertical);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBrushButton("╚ Cua TR", CellType.ArrowCurveTopRight);
            DrawBrushButton("╝ Cua TL", CellType.ArrowCurveTopLeft);
            DrawBrushButton("╔ Cua BR", CellType.ArrowCurveBottomRight);
            DrawBrushButton("╗ Cua BL", CellType.ArrowCurveBottomLeft);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBrushButton("• Ô Trống (Sàn)", CellType.EmptyDot);
            DrawBrushButton("X Xóa Ô (Hố sâu)", CellType.None);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // --- 3. ĐIỀU KHIỂN ZOOM VÀ LƯU ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("🔍 Zoom:", GUILayout.Width(50));
            cellSize = GUILayout.HorizontalSlider(cellSize, 30f, 100f, GUILayout.ExpandWidth(true));
        
            if (GUILayout.Button("💾 LƯU LEVEL", GUILayout.Width(150), GUILayout.Height(30)))
            {
                SaveGridToData();
            }
            GUILayout.EndHorizontal();

            DrawGrid();
        }

        private void DrawBrushButton(string label, CellType type)
        {
            Color oldColor = GUI.backgroundColor;
            if (brushType == type) GUI.backgroundColor = Color.yellow;
            else GUI.backgroundColor = GetColorForType(type);

            if (GUILayout.Button(label, GUILayout.Height(25))) brushType = type;
            GUI.backgroundColor = oldColor; 
        }

        private void DrawGrid()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
        
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(cellSize * 0.25f)); 

            for (int y = currentLevel.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); 

                for (int x = 0; x < currentLevel.width; x++)
                {
                    EditorCell cell = tempGrid[x, y];
                
                    GUI.backgroundColor = GetColorForType(cell.type);
                    string label = GetLabelForType(cell.type, cell.id);

                    if (GUILayout.Button(label, btnStyle, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        ApplySmartBrush(x, y, cell);
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        
            GUI.backgroundColor = Color.white; 
            EditorGUILayout.EndScrollView(); 
        }

        private void ApplySmartBrush(int x, int y, EditorCell cell)
        {
            if (brushType == CellType.None || brushType == CellType.EmptyDot)
            {
                cell.type = brushType; cell.id = ""; 
            }
            else if (IsArrowHead(brushType))
            {
                cell.type = brushType; cell.id = GetNextAvailableID(); 
            }
            else 
            {
                string adjacentID = GetAdjacentArrowID(x, y);
                if (!string.IsNullOrEmpty(adjacentID)) {
                    cell.type = brushType; cell.id = adjacentID; 
                } else {
                    EditorUtility.DisplayDialog("Vẽ lỗi", "Phần Thân, Đuôi hoặc Khúc Cua phải được vẽ sát cạnh một phần mũi tên đã có sẵn!", "Đã hiểu");
                }
            }
        }

        private string GetNextAvailableID() {
            int maxId = 0;
            for (int x = 0; x < currentLevel.width; x++)
            for (int y = 0; y < currentLevel.height; y++)
                if (int.TryParse(tempGrid[x, y].id, out int parsedId))
                    if (parsedId > maxId) maxId = parsedId;
            return (maxId + 1).ToString();
        }

        private string GetAdjacentArrowID(int x, int y) {
            if (x > 0 && !string.IsNullOrEmpty(tempGrid[x - 1, y].id)) return tempGrid[x - 1, y].id;
            if (x < currentLevel.width - 1 && !string.IsNullOrEmpty(tempGrid[x + 1, y].id)) return tempGrid[x + 1, y].id;
            if (y > 0 && !string.IsNullOrEmpty(tempGrid[x, y - 1].id)) return tempGrid[x, y - 1].id;
            if (y < currentLevel.height - 1 && !string.IsNullOrEmpty(tempGrid[x, y + 1].id)) return tempGrid[x, y + 1].id;
            return ""; 
        }

        private bool IsArrowHead(CellType type) {
            return type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown || 
                   type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;
        }

        private bool IsArrowTail(CellType type) {
            return type == CellType.ArrowTailUp || type == CellType.ArrowTailDown || 
                   type == CellType.ArrowTailLeft || type == CellType.ArrowTailRight;
        }

        // --- LOGIC LƯU DATA MỚI (FIX 2) ---
        private void SaveGridToData() {
            if (!ValidateGridLogic()) return; 

            currentLevel.cells.Clear();
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    // BỎ LỆNH IF KIỂM TRA NONE Ở ĐÂY.
                    // Bắt buộc LƯU TOÀN BỘ MA TRẬN 100% để JSON có mốc tọa độ chính xác.
                    currentLevel.cells.Add(new CellData(x, y, tempGrid[x, y].type, tempGrid[x, y].id));
                }
            }
            EditorUtility.SetDirty(currentLevel); 
            AssetDatabase.SaveAssets(); 
            Debug.Log($"<color=#00FF00>Đã lưu Level {currentLevel.levelID} thành công (Bao gồm cả các ô None)!</color>");
        }

        private bool ValidateGridLogic() {
            Dictionary<string, int> headCountPerID = new Dictionary<string, int>();
            Dictionary<string, int> tailCountPerID = new Dictionary<string, int>(); 
            Dictionary<string, List<Vector2Int>> positionsPerID = new Dictionary<string, List<Vector2Int>>();
            Dictionary<string, Vector2Int> headPositionPerID = new Dictionary<string, Vector2Int>(); 

            for (int x = 0; x < currentLevel.width; x++) {
                for (int y = 0; y < currentLevel.height; y++) {
                    string id = tempGrid[x, y].id;
                    if (string.IsNullOrEmpty(id)) continue;

                    if (!headCountPerID.ContainsKey(id)) {
                        headCountPerID[id] = 0;
                        tailCountPerID[id] = 0;
                        positionsPerID[id] = new List<Vector2Int>();
                    }
                    Vector2Int pos = new Vector2Int(x, y);
                    positionsPerID[id].Add(pos);

                    if (IsArrowHead(tempGrid[x, y].type)) {
                        headCountPerID[id]++;
                        headPositionPerID[id] = pos; 
                    }
                    if (IsArrowTail(tempGrid[x, y].type)) tailCountPerID[id]++;
                }
            }

            foreach (var kvp in headCountPerID) {
                string id = kvp.Key;
                int headsCount = kvp.Value;
                int tailsCount = tailCountPerID[id];
                List<Vector2Int> positions = positionsPerID[id];
                int totalNodes = positions.Count;

                if (headsCount == 0) { EditorUtility.DisplayDialog("Lỗi Map!", $"Mũi tên (ID: {id}) KHÔNG CÓ ĐẦU!", "Sửa ngay"); return false; }
                if (headsCount > 1) { EditorUtility.DisplayDialog("Lỗi Map!", $"Mũi tên (ID: {id}) có tới {headsCount} cái Đầu!", "Sửa ngay"); return false; }
                if (tailsCount == 0) { EditorUtility.DisplayDialog("Lỗi Map!", $"Mũi tên (ID: {id}) KHÔNG CÓ ĐUÔI!", "Sửa ngay"); return false; }
                if (tailsCount > 1) { EditorUtility.DisplayDialog("Lỗi Map!", $"Mũi tên (ID: {id}) có tới {tailsCount} cái Đuôi!", "Sửa ngay"); return false; }
                if (totalNodes < 2) { EditorUtility.DisplayDialog("Lỗi Map!", $"Mũi tên (ID: {id}) quá ngắn!", "Sửa ngay"); return false; }

                int branchCount = 0;    
                int endPointsCount = 0; 
                foreach (var pos in positions) {
                    int neighbors = 0;
                    if (positions.Contains(new Vector2Int(pos.x + 1, pos.y))) neighbors++;
                    if (positions.Contains(new Vector2Int(pos.x - 1, pos.y))) neighbors++;
                    if (positions.Contains(new Vector2Int(pos.x, pos.y + 1))) neighbors++;
                    if (positions.Contains(new Vector2Int(pos.x, pos.y - 1))) neighbors++;
                    if (neighbors >= 3) branchCount++;
                    if (neighbors == 1) endPointsCount++;
                }

                if (branchCount > 0) { EditorUtility.DisplayDialog("Lỗi Phân Nhánh!", $"Mũi tên (ID: {id}) bị phân nhánh (+, T, H)!", "Sửa ngay"); return false; }
                if (endPointsCount != 2) { EditorUtility.DisplayDialog("Lỗi Vòng Lặp!", $"Mũi tên (ID: {id}) tạo vòng khép kín!", "Sửa ngay"); return false; }

                HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(headPositionPerID[id]); 
                visited.Add(headPositionPerID[id]);
                while (queue.Count > 0) {
                    Vector2Int curr = queue.Dequeue();
                    Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    foreach (var dir in dirs) {
                        Vector2Int next = curr + dir;
                        if (positions.Contains(next) && !visited.Contains(next)) {
                            visited.Add(next); queue.Enqueue(next);
                        }
                    }
                }
                if (visited.Count != totalNodes) { EditorUtility.DisplayDialog("Lỗi Đứt Đoạn!", $"Mũi tên (ID: {id}) bị đứt khúc!", "Sửa ngay"); return false; }
            }
            return true; 
        }

        private void LoadDataToGrid() {
            tempGrid = new EditorCell[currentLevel.width, currentLevel.height];
            for (int x = 0; x < currentLevel.width; x++)
            for (int y = 0; y < currentLevel.height; y++)
                tempGrid[x, y] = new EditorCell(); // Tạo mới 100% là None

            foreach (var cellData in currentLevel.cells) {
                if (cellData.x < currentLevel.width && cellData.y < currentLevel.height) {
                    tempGrid[cellData.x, cellData.y].type = cellData.type;
                    tempGrid[cellData.x, cellData.y].id = cellData.arrowID;
                }
            }
        }

        private string GetLabelForType(CellType type, string id) {
            string displayID = string.IsNullOrEmpty(id) ? "" : $" \n({id})";
            switch (type) {
                case CellType.None: return "";
                case CellType.EmptyDot: return "•";
                case CellType.ArrowHeadUp: return $"↑{displayID}";
                case CellType.ArrowHeadDown: return $"↓{displayID}";
                case CellType.ArrowHeadLeft: return $"←{displayID}";
                case CellType.ArrowHeadRight: return $"→{displayID}";
                case CellType.ArrowTailUp: return $"╨{displayID}";
                case CellType.ArrowTailDown: return $"╥{displayID}";
                case CellType.ArrowTailLeft: return $"╡{displayID}";
                case CellType.ArrowTailRight: return $"╞{displayID}";
                case CellType.ArrowBodyHorizontal: return $"═{displayID}"; 
                case CellType.ArrowBodyVertical: return $"║{displayID}"; 
                case CellType.ArrowCurveTopRight: return $"╚{displayID}"; 
                case CellType.ArrowCurveTopLeft: return $"╝{displayID}"; 
                case CellType.ArrowCurveBottomRight: return $"╔{displayID}"; 
                case CellType.ArrowCurveBottomLeft: return $"╗{displayID}"; 
                default: return "";
            }
        }

        private Color GetColorForType(CellType type) {
            switch (type) {
                case CellType.None: return new Color(0.2f, 0.2f, 0.2f); 
                case CellType.EmptyDot: return Color.gray;              
                case CellType.ArrowHeadUp:
                case CellType.ArrowHeadDown:
                case CellType.ArrowHeadLeft:
                case CellType.ArrowHeadRight: return new Color(0.3f, 0.9f, 0.3f); 
                case CellType.ArrowTailUp:
                case CellType.ArrowTailDown:
                case CellType.ArrowTailLeft:
                case CellType.ArrowTailRight: return new Color(0.2f, 0.5f, 0.9f); 
                case CellType.ArrowBodyHorizontal:
                case CellType.ArrowBodyVertical: return new Color(0.6f, 0.8f, 1f); 
                case CellType.ArrowCurveTopRight:
                case CellType.ArrowCurveTopLeft:
                case CellType.ArrowCurveBottomRight:
                case CellType.ArrowCurveBottomLeft: return new Color(1f, 0.7f, 0.3f); 
                default: return Color.white;            
            }
        }
    }
}
#endif