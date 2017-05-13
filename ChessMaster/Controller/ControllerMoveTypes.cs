using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.Controller
{
    /// <summary>
    /// Possible types of movement that controller
    /// can parse to basic move commands
    /// </summary>
    public enum ControllerMoveTypes
    {
        MOVE2EMPTY,
        MOVE_AND_KILL,
        CASTLING,
        NONE,
    }
}
