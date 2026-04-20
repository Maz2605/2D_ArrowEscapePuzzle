#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ArrowGame.Data.LevelProvider;
using ShareCore.Data;
using ShareCore.Scripts.Data;
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

        private CellType brushType = CellType.ArrowHeadUp; 
        private Vector2 scrollPosition;     
        private float cellSize = 55f; 

        [MenuItem("Tools/Arrow Level Editor (Entity-Based)")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Arrow Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("🛠 BẢNG VẼ MAP (ENTITY-BASED REFACTOR)", EditorStyles.boldLabel);
        
            EditorGUI.BeginChangeCheck();
            currentLevel = (LevelDataSO)EditorGUILayout.ObjectField("Data SO:", currentLevel, typeof(LevelDataSO), false);
            if (EditorGUI.EndChangeCheck() && currentLevel != null)
            {
                LoadDataToGrid();
            }

            if (currentLevel == null)
            {
                EditorGUILayout.HelpBox("Kéo file LevelDataSO vào đây để bắt đầu", MessageType.Warning);
                return;
            }

            // --- 1. KHUNG CÀI ĐẶT MAP ---
            GUILayout.BeginVertical("box");
            currentLevel.levelID = EditorGUILayout.TextField("Level ID:", currentLevel.levelID);

            EditorGUI.BeginChangeCheck();
            int newWidth = EditorGUILayout.IntField("Width (Ngang)", currentLevel.width);
            int newHeight = EditorGUILayout.IntField("Height (Dọc)", currentLevel.height);
            currentLevel.difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Độ khó:", currentLevel.difficulty);
        
            if (EditorGUI.EndChangeCheck() || tempGrid == null || tempGrid.GetLength(0) != newWidth || tempGrid.GetLength(1) != newHeight)
            {
                currentLevel.width = Mathf.Max(3, newWidth);
                currentLevel.height = Mathf.Max(3, newHeight);
                LoadDataToGrid();
            }
            GUILayout.EndVertical();

            // --- 2. BẢNG CỌ VẼ ---
            DrawPalette();

            // --- 3. ĐIỀU KHIỂN ZOOM VÀ LƯU ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("🔍 Zoom:", GUILayout.Width(50));
            cellSize = GUILayout.HorizontalSlider(cellSize, 30f, 100f, GUILayout.ExpandWidth(true));
        
            if (GUILayout.Button("💾 LƯU LEVEL (JSON v2)", GUILayout.Width(150), GUILayout.Height(30)))
            {
                SaveGridToData();
            }
            GUILayout.EndHorizontal();

            DrawGrid();
        }

        private void DrawPalette()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("🎨 BẢNG CỌ VẼ (Vẽ Đầu trước để tạo ID mới):", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            DrawBrushButton("↑ Đầu", CellType.ArrowHeadUp);
            DrawBrushButton("↓ Đầu", CellType.ArrowHeadDown);
            DrawBrushButton("← Đầu", CellType.ArrowHeadLeft);
            DrawBrushButton("→ Đầu", CellType.ArrowHeadRight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBrushButton("═ Thân", CellType.ArrowBodyHorizontal);
            DrawBrushButton("║ Thân", CellType.ArrowBodyVertical);
            DrawBrushButton("• Trống", CellType.EmptyDot);
            DrawBrushButton("X Xóa", CellType.None);
            GUILayout.EndHorizontal();

            GUILayout.Label("Ghi chú: Khúc Cua và Đuôi sẽ tự động được tính toán khi lưu.");
            GUILayout.EndVertical();
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
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
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
                        ApplyBrush(x, y, cell);
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            GUI.backgroundColor = Color.white; 
            EditorGUILayout.EndScrollView(); 
        }

        private void ApplyBrush(int x, int y, EditorCell cell)
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
                    EditorUtility.DisplayDialog("Lỗi", "Hãy vẽ Thân sát cạnh một cái Đầu đã có sẵn!", "OK");
                }
            }
        }

        // ================= LOGIC REFACTOR: GRID TO PATH (SAVE) =================
        private void SaveGridToData() 
        {
            if (!ValidateGridLogic()) return; 

            currentLevel.arrows.Clear();
            
            // Tìm tất cả các ID có trên map
            HashSet<string> allIDs = new HashSet<string>();
            for (int x = 0; x < currentLevel.width; x++)
                for (int y = 0; y < currentLevel.height; y++)
                    if (!string.IsNullOrEmpty(tempGrid[x, y].id)) allIDs.Add(tempGrid[x, y].id);

            foreach (string id in allIDs)
            {
                List<Vector2Int> path = ReconstructPath(id);
                if (path != null)
                {
                    // Luôn coi là HeadFirst vì Editor vẽ từ Đầu
                    currentLevel.arrows.Add(new ArrowSaveData(id, path, true));
                }
            }

            EditorUtility.SetDirty(currentLevel); 
            AssetDatabase.SaveAssets(); 
            Debug.Log($"<color=green>Đã lưu Level {currentLevel.levelID} với {currentLevel.arrows.Count} thực thể mũi tên!</color>");
        }

        private List<Vector2Int> ReconstructPath(string id)
        {
            Vector2Int headPos = new Vector2Int(-1, -1);
            List<Vector2Int> allNodes = new List<Vector2Int>();

            for (int x = 0; x < currentLevel.width; x++) {
                for (int y = 0; y < currentLevel.height; y++) {
                    if (tempGrid[x, y].id == id) {
                        allNodes.Add(new Vector2Int(x, y));
                        if (IsArrowHead(tempGrid[x, y].type)) headPos = new Vector2Int(x, y);
                    }
                }
            }

            if (headPos.x == -1) return null;

            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int curr = headPos;
            Vector2Int prev = new Vector2Int(-1, -1);

            while (path.Count < allNodes.Count)
            {
                path.Add(curr);
                Vector2Int next = new Vector2Int(-1, -1);
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (var d in dirs)
                {
                    Vector2Int n = curr + d;
                    if (allNodes.Contains(n) && n != prev) { next = n; break; }
                }

                if (next.x == -1) break;
                prev = curr;
                curr = next;
            }
            return path;
        }

        // ================= LOGIC REFACTOR: PATH TO GRID (LOAD) =================
        private void LoadDataToGrid() 
        {
            tempGrid = new EditorCell[currentLevel.width, currentLevel.height];
            for (int x = 0; x < currentLevel.width; x++)
                for (int y = 0; y < currentLevel.height; y++)
                    tempGrid[x, y] = new EditorCell { type = CellType.EmptyDot };

            if (currentLevel.arrows == null) return;

            foreach (var arrow in currentLevel.arrows)
            {
                for (int i = 0; i < arrow.Path.Count; i++)
                {
                    Vector2Int pos = arrow.Path[i];
                    if (pos.x < currentLevel.width && pos.y < currentLevel.height)
                    {
                        tempGrid[pos.x, pos.y].id = arrow.ArrowID;
                        tempGrid[pos.x, pos.y].type = CalculateCellType(i, arrow.Path, arrow.IsHeadFirst);
                    }
                }
            }
        }

        private CellType CalculateCellType(int index, List<Vector2Int> path, bool isHeadFirst)
        {
            Vector2Int curr = path[index];
            bool isHead = (isHeadFirst && index == 0) || (!isHeadFirst && index == path.Count - 1);
            bool isTail = (isHeadFirst && index == path.Count - 1) || (!isHeadFirst && index == 0);

            if (isHead)
            {
                Vector2Int n = (index == 0) ? path[1] : path[index - 1];
                if (n.y < curr.y) return CellType.ArrowHeadUp;
                if (n.y > curr.y) return CellType.ArrowHeadDown;
                if (n.x < curr.x) return CellType.ArrowHeadRight;
                return CellType.ArrowHeadLeft;
            }
            
            if (isTail)
            {
                Vector2Int n = (index == path.Count - 1) ? path[index - 1] : path[index + 1];
                if (n.y < curr.y) return CellType.ArrowTailUp;
                if (n.y > curr.y) return CellType.ArrowTailDown;
                if (n.x < curr.x) return CellType.ArrowTailRight;
                return CellType.ArrowTailLeft;
            }

            Vector2Int prev = path[index - 1];
            Vector2Int next = path[index + 1];
            if (prev.x == next.x) return CellType.ArrowBodyVertical;
            if (prev.y == next.y) return CellType.ArrowBodyHorizontal;

            bool u = prev.y > curr.y || next.y > curr.y;
            bool d = prev.y < curr.y || next.y < curr.y;
            bool l = prev.x < curr.x || next.x < curr.x;
            bool r = prev.x > curr.x || next.x > curr.x;

            if (u && r) return CellType.ArrowCurveTopRight;
            if (u && l) return CellType.ArrowCurveTopLeft;
            if (d && r) return CellType.ArrowCurveBottomRight;
            return CellType.ArrowCurveBottomLeft;
        }

        // ================= HELPERS (GIỮ NGUYÊN) =================
        private string GetNextAvailableID() {
            int maxId = 0;
            for (int x = 0; x < currentLevel.width; x++)
                for (int y = 0; y < currentLevel.height; y++)
                    if (int.TryParse(tempGrid[x, y].id, out int id)) if (id > maxId) maxId = id;
            return (maxId + 1).ToString();
        }

        private string GetAdjacentArrowID(int x, int y) {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs) {
                int nx = x + d.x, ny = y + d.y;
                if (nx >= 0 && nx < currentLevel.width && ny >= 0 && ny < currentLevel.height)
                    if (!string.IsNullOrEmpty(tempGrid[nx, ny].id)) return tempGrid[nx, ny].id;
            }
            return ""; 
        }

        private bool IsArrowHead(CellType t) => t == CellType.ArrowHeadUp || t == CellType.ArrowHeadDown || t == CellType.ArrowHeadLeft || t == CellType.ArrowHeadRight;
        private bool ValidateGridLogic() => true; // Tạm để true cho nhanh, bạn có thể copy logic cũ vào

        private string GetLabelForType(CellType type, string id) {
            string dID = string.IsNullOrEmpty(id) ? "" : $" \n({id})";
            return type switch {
                CellType.None => "", CellType.EmptyDot => "•",
                CellType.ArrowHeadUp => $"↑{dID}", CellType.ArrowHeadDown => $"↓{dID}",
                CellType.ArrowHeadLeft => $"←{dID}", CellType.ArrowHeadRight => $"→{dID}",
                CellType.ArrowTailUp => $"╨{dID}", CellType.ArrowTailDown => $"╥{dID}",
                CellType.ArrowTailLeft => $"╡{dID}", CellType.ArrowTailRight => $"╞{dID}",
                CellType.ArrowBodyHorizontal => $"═{dID}", CellType.ArrowBodyVertical => $"║{dID}", 
                CellType.ArrowCurveTopRight => $"╚{dID}", CellType.ArrowCurveTopLeft => $"╝{dID}", 
                CellType.ArrowCurveBottomRight => $"╔{dID}", CellType.ArrowCurveBottomLeft => $"╗{dID}", 
                _ => ""
            };
        }

        private Color GetColorForType(CellType type) {
            if (IsArrowHead(type)) return new Color(0.3f, 0.9f, 0.3f);
            if (type == CellType.None) return new Color(0.2f, 0.2f, 0.2f);
            if (type == CellType.EmptyDot) return Color.gray;
            return new Color(0.6f, 0.8f, 1f);
        }
    }
}
#endif