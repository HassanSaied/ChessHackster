using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Class responsible for parsing the last state change in ChessEngine to 
/// simple commands sent to the motors controller module
/// </summary>
namespace ChessEngine.Controller
{
    public class Controller
    {

        private Engine.MoveContent _currMove;
        private ControllerMoveTypes _currMoveType;
        private List<byte[]> _currCommands;
        private Dictionary<ControllerPieceType, Tuple<bool[], byte[][]>> _deadPieces;
        private Tuple<bool, byte[]>[] _deadPawns;
        private const byte _numberOfCells = 8;
        /// <summary>
        /// Basic constructor 
        /// </summary>
        public Controller()
        {
            // Assign places for dead pieces
            // Pawns
            _deadPawns = new Tuple<bool, byte[]>[2];
            Tuple<bool, byte[]> tempTuple = new Tuple<bool, byte[]>(false, new byte[2]);
            // Left side
            for (byte i = 0; i < 10; ++i)
            {
                tempTuple.Item2[0] = 0;
                tempTuple.Item2[1] = i;
                _deadPawns[i] = tempTuple;
            }
            // Right side
            for (byte i = 0; i < 10; ++i)
            {
                tempTuple.Item2[0] = 7;
                tempTuple.Item2[1] = i;
                _deadPawns[i] = tempTuple;
            }

            // King and Queen
            _deadPieces = new Dictionary<ControllerPieceType, Tuple<bool[], byte[][]>>();
            ControllerPieceType tempPieceType;
            byte[][] tempPos = new byte[1][];
            tempPos[0] = new byte[2];
            tempPos[1] = new byte[2];
            bool[] tempBool = new bool[] { false, false };
            Tuple<bool[], byte[][]> tempTuple1 = new Tuple<bool[], byte[][]>(tempBool, tempPos);
            // White
            tempPieceType._pieceColor = Engine.ChessPieceColor.White;
            // Queen
            tempPieceType._pieceType = Engine.ChessPieceType.Queen;
            tempPos[0][0] = 0;
            tempPos[0][1] = 4;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // King
            tempPieceType._pieceType = Engine.ChessPieceType.King;
            tempPos[0][0] = 0;
            tempPos[0][1] = 5;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // Black
            tempPieceType._pieceColor = Engine.ChessPieceColor.Black;
            // Queen
            tempPieceType._pieceType = Engine.ChessPieceType.Queen;
            tempPos[0][0] = 9;
            tempPos[0][1] = 5;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // King
            tempPieceType._pieceType = Engine.ChessPieceType.King;
            tempPos[0][0] = 9;
            tempPos[0][1] = 4;
            _deadPieces.Add(tempPieceType, tempTuple1);

            // Others
            tempPos = new byte[2][];
            tempPos[0] = new byte[2];
            tempPos[1] = new byte[2];
            // White
            tempPieceType._pieceColor = Engine.ChessPieceColor.White;
            // Rook 
            tempPieceType._pieceType = Engine.ChessPieceType.Rook;
            tempPos[0][0] = 0;
            tempPos[1][0] = 0;
            tempPos[0][1] = 1;
            tempPos[1][1] = 8;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // Knight
            tempPieceType._pieceType = Engine.ChessPieceType.Knight;
            tempPos[0][0] = 0;
            tempPos[1][0] = 0;
            tempPos[0][1] = 2;
            tempPos[1][1] = 7;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // Bishop
            tempPieceType._pieceType = Engine.ChessPieceType.Bishop;
            tempPos[0][0] = 0;
            tempPos[1][0] = 0;
            tempPos[0][1] = 3;
            tempPos[1][1] = 6;
            _deadPieces.Add(tempPieceType, tempTuple1);

            // Black
            tempPieceType._pieceColor = Engine.ChessPieceColor.White;
            // Rook 
            tempPieceType._pieceType = Engine.ChessPieceType.Rook;
            tempPos[0][0] = 9;
            tempPos[1][0] = 9;
            tempPos[0][1] = 1;
            tempPos[1][1] = 8;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // Knight
            tempPieceType._pieceType = Engine.ChessPieceType.Knight;
            tempPos[0][0] = 9;
            tempPos[1][0] = 9;
            tempPos[0][1] = 2;
            tempPos[1][1] = 7;
            _deadPieces.Add(tempPieceType, tempTuple1);
            // Bishop
            tempPieceType._pieceType = Engine.ChessPieceType.Bishop;
            tempPos[0][0] = 9;
            tempPos[1][0] = 9;
            tempPos[0][1] = 3;
            tempPos[1][1] = 6;
            _deadPieces.Add(tempPieceType, tempTuple1);

        }

        /// <summary>
        /// Main function of class that takes a state of chess engine 
        /// and parse it to basic movement commands for motor controller
        /// </summary>
        /// <remarks>
        /// Flow:
        /// 1- Take last move as an input
        /// 2- Parse last move to a series of basic move commands
        /// 3- Send a basic move command to motor controller via I2C
        /// 4- Wait for End of Movement signal from motor controller via I2C
        /// 5- If still exists another basic move command jump to 3
        /// 6- End 
        /// </remarks>
        /// <param name="lastMove"> Last Chess Engine state </param>

        public void simulate(Engine.MoveContent lastMove)
        {
            _currMove = lastMove;
            determineMovementType();
            generateCommands();
            sendCommands();
        }

        /// <summary>
        /// Determines last move type
        /// </summary>
        private void determineMovementType()
        {
            // Check any piece moved
            if (!_currMove.MovingPiecePrimary.Moved)
            {
                _currMoveType = ControllerMoveTypes.NONE;
                return;
            }

            // Check if another piece killed
            if (_currMove.TakenPiece.PieceType != Engine.ChessPieceType.None || _currMove.EnPassantOccured)
            {
                _currMoveType = ControllerMoveTypes.MOVE_AND_KILL;
                return;
            }

            // Check for castling
            if (_currMove.MovingPieceSecondary.PieceType == Engine.ChessPieceType.Rook)
            {
                _currMoveType = ControllerMoveTypes.CASTLING;
                return;
            }

            // Ony moving to empty remains 
            _currMoveType = ControllerMoveTypes.MOVE2EMPTY;

        }

        /// <summary>
        /// Generates basic movement commands based on
        /// current move type
        /// </summary>
        private void generateCommands()
        {
            _currCommands = new List<byte[]>();
            switch (_currMoveType)
            {
                case (ControllerMoveTypes.MOVE2EMPTY):
                    _currCommands.Add(get2DPos(_currMove.MovingPiecePrimary.SrcPosition).Concat(
                         get2DPos(_currMove.MovingPiecePrimary.DstPosition)).ToArray());
                    checkPromotion();
                    break;

                case (ControllerMoveTypes.MOVE_AND_KILL):
                    ControllerPieceType tempKilledPiece;
                    tempKilledPiece._pieceColor = _currMove.TakenPiece.PieceColor;
                    tempKilledPiece._pieceType = _currMove.TakenPiece.PieceType;
                    killPiece(_currMove.TakenPiece.Position, tempKilledPiece);
                    _currCommands.Add(get2DPos(_currMove.MovingPiecePrimary.SrcPosition).Concat(
                        get2DPos(_currMove.MovingPiecePrimary.DstPosition)).ToArray());
                    checkPromotion();
                    break;

                case (ControllerMoveTypes.CASTLING):
                    _currCommands.Add(get2DPos(_currMove.MovingPiecePrimary.SrcPosition).Concat(
                         get2DPos(_currMove.MovingPiecePrimary.DstPosition)).ToArray());
                    _currCommands.Add(get2DPos(_currMove.MovingPieceSecondary.SrcPosition).Concat(
                        get2DPos(_currMove.MovingPieceSecondary.DstPosition)).ToArray());
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Sends generated commands to motor controller 
        /// via I2C
        /// </summary>
        private void sendCommands()
        {
            for (int i = 0; i < _currCommands.Count; ++i)
            {
                // TODO: call Serial Communication 
            }
        }

        /// <summary>
        /// A simple parser from 1D array supported by chess engine to 
        /// 2D array vied by motor controller 
        /// </summary>
        /// <param name="pos1D"> Position provided by chess engine </param>
        /// <returns></returns>
        private byte[] get2DPos(byte pos1D)
        {
            byte[] pos2D = new byte[2];
            // row
            pos2D[0] = (byte)(8 - (pos1D / _numberOfCells));
            // col
            pos2D[1] = (byte)(8 -(pos1D % _numberOfCells));
            return pos2D;
        }

        /// <summary>
        /// Check if promotion occured and adds corresponding basic
        /// movement command
        /// </summary>
        private void checkPromotion()
        {
            if (_currMove.PawnPromotedTo != Engine.ChessPieceType.None)
            {
                ControllerPieceType tempPiece;
                tempPiece._pieceColor = _currMove.MovingPiecePrimary.PieceColor;
                tempPiece._pieceType = _currMove.MovingPiecePrimary.PieceType;
                for (int i = 0; i < _deadPieces[tempPiece].Item2.Length; ++i)
                {
                    if (_deadPieces[tempPiece].Item1[i])
                    {
                        killPiece(_currMove.MovingPiecePrimary.DstPosition,
                            tempPiece);
                        _currCommands.Add(_deadPieces[tempPiece].Item2[i].Concat(
                            get2DPos(_currMove.MovingPiecePrimary.DstPosition)).ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Removes piece from board to its corresponding place outside
        /// </summary>
        /// <param name="piecePos"> The position of the piece to kill </param>
        /// <param name="pieceType"> The type and color of the piece to kill </param>
        private void killPiece(byte piecePos, ControllerPieceType pieceType)
        {
            if (pieceType._pieceType != Engine.ChessPieceType.Pawn)
            {
                for (int i = 0; i < _deadPieces[pieceType].Item2.Length; ++i)
                {
                    if (!_deadPieces[pieceType].Item1[i])
                    {
                        _currCommands.Add(get2DPos(piecePos).Concat(
                           _deadPieces[pieceType].Item2[i]).ToArray());
                        _deadPieces[pieceType].Item1[i] = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _deadPawns.Length; ++i)
                {
                    if (!_deadPawns[i].Item1)
                    {
                        _currCommands.Add(get2DPos(piecePos).Concat(
                          _deadPawns[i].Item2).ToArray());
                        _deadPawns[i] = new Tuple<bool, byte[]>(true, _deadPawns[i].Item2);
                    }
                }
            }
        }
    }
}
