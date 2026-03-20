using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic
{
    public class ArrowData
    {
        public string ID { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public CellType Type { get; set; }
        
        public ArrowData(string id, int x, int y, CellType type)
        {
            ID = id;
            X = x;
            Y = y;
            Type = type;
        }

        public void SetData(string id, CellType type)
        {
            ID = id;
            Type = type;
        }
        
        public void ResetData()
        {
            ID = "";
            Type = CellType.EmptyDot;
        }
    }
}