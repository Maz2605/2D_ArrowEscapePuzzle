// EditorUIManager đã được thay thế bởi EditorController.
// File này giữ lại để tránh missing script errors trong scene.
// Xóa component này khỏi scene và thêm EditorController thay thế.
using UnityEngine;

namespace EditorTool.Scripts.UI
{
    [System.Obsolete("Sử dụng EditorController thay thế. File này sẽ bị xóa.")]
    public class EditorUIManager : MonoBehaviour { }
}