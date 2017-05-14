using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.Controller
{
    /// <summary>
    /// Used by dictionary for promotion
    /// </summary>
    public struct ControllerPieceType
    {
        public Engine.ChessPieceType  _pieceType;
        public Engine.ChessPieceColor _pieceColor;
    }
}
