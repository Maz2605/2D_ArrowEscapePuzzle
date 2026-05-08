using System.Collections.Generic;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public static class SpecialCellLogicFactory
    {
        private static readonly Dictionary<BoardSpecialType, ISpecialCellLogic> _logics = new Dictionary<BoardSpecialType, ISpecialCellLogic>()
        {
            { BoardSpecialType.Redirect, new RedirectLogic() },
            { BoardSpecialType.Portal, new PortalLogic() }
        };

        public static ISpecialCellLogic GetLogic(BoardSpecialType type)
        {
            _logics.TryGetValue(type, out ISpecialCellLogic logic);
            return logic;
        }
    }
}
