using ChessEngine.Communication.Serial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using System.Threading;

/// <summary>
/// Class responsible for parsing the last state change in ChessEngine to
/// simple commands sent to the motors controller module
/// </summary>
namespace ChessEngine.Controller
{
    public class Controller
    {
        private enum Cancel  {ENABLED, DISABLED, PENDING};
        // Custom Pair
        private class Pair
        {
           public bool   _state;
           public byte[] _position; 

           public Pair(bool s, byte[] p)
            {
                _state = s;
                _position = p;
            }
        }

        // For moves and related commands
        private Engine.MoveContent _currMove;
        private ControllerMoveTypes _currMoveType;
        private List<byte[]> _currCommands;

        // For killing and promotion
        private Dictionary<ControllerPieceType, Pair[]> _deadPieces; 
        private Pair[] _deadPawns;
        
        // Constants
        private const byte BLOCK_SIZE = 40;
        private const byte MAX_PER_BYTE = 248;
        private const byte _numberOfCells = 8;

        // Signals
        private const byte SIG_BOM = 249;
        private const byte SIG_EOM  = 250;
        private const byte SIG_LOAD  = 251;
        private const byte SIG_SAVE   = 252;
        public const byte SIG_VALIDATE= 253;
        private const byte SIG_CONFIRM  = 254;
        private const byte SIG_CANCEL   = 255;

        // For saving moves
        byte[] storedBytes;

        // Shared object and related
        private System.Object _lock = new System.Object();
        private Cancel _cancel = Cancel.ENABLED;
        public void setCancel()
        {
            if (_cancel == Cancel.ENABLED)
                _cancel = Cancel.PENDING;
        }
        private bool _inProgress = false;
        public bool inProgress
        {
            get { return _inProgress; }
        }

        /// <summary>
        /// Basic constructor
        /// </summary>
        public Controller()
        {
            // Busy till validate
            _inProgress = true;

            storedBytes = new byte[24];

            // Start communication
            initiateCommunication();

            // Assign places for dead pieces
            // Pawns
            _deadPawns = new Pair[16];
            for (byte i = 1; i<=8; ++i)
            {
                _deadPawns[i - 1] = new Pair(false, new byte[] { 0, i });
                _deadPawns[i + 7] = new Pair(false, new byte[] { 9, i }); 
            }

            // Other pieces
            _deadPieces = new Dictionary<ControllerPieceType, Pair[]>();
            ControllerPieceType tempPieceType;
            // white
            tempPieceType._pieceColor = Engine.ChessPieceColor.White;
            // Rooks
            tempPieceType._pieceType = Engine.ChessPieceType.Rook;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 8, 9 }), new Pair(false, new byte[] { 7, 9 }) });
            // Knights
            tempPieceType._pieceType = Engine.ChessPieceType.Knight;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 6, 9 }), new Pair(false, new byte[] { 5, 9 }) });
            // Bishops
            tempPieceType._pieceType = Engine.ChessPieceType.Bishop;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 4, 9 }), new Pair(false, new byte[] { 3, 9 }) });
            // Queen
            tempPieceType._pieceType = Engine.ChessPieceType.Queen;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 2, 9 }) });
            // King
            tempPieceType._pieceType = Engine.ChessPieceType.King;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 1, 9 }) });

            // black
            tempPieceType._pieceColor = Engine.ChessPieceColor.Black;
            // Rooks
            tempPieceType._pieceType = Engine.ChessPieceType.Rook;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 8, 0 }), new Pair(false, new byte[] { 7, 0 }) });
            // Knights
            tempPieceType._pieceType = Engine.ChessPieceType.Knight;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 6, 0 }), new Pair(false, new byte[] { 5, 0 }) });
            // Bishops
            tempPieceType._pieceType = Engine.ChessPieceType.Bishop;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 4, 0 }), new Pair(false, new byte[] { 3, 0 }) });
            // Queen
            tempPieceType._pieceType = Engine.ChessPieceType.Queen;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 2, 0 }) });
            // King
            tempPieceType._pieceType = Engine.ChessPieceType.King;
            _deadPieces.Add(tempPieceType, new Pair[] { new Pair(false, new byte[] { 1, 0 }) });

        }

        ~Controller()
        {

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

        public async Task<bool> simulate(Engine.MoveContent lastMove)
        {
            _currMove = lastMove;
            determineMovementType();
            generateCommands();
            await executeCommands();
            // TODO: play soundtracks
            return _cancel == Cancel.DISABLED;
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
                    ControllerPieceType tempKilledPiece = new ControllerPieceType();
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

        private async Task initiateCommunication()
        {
            await SerialManager.Initiate();

            do
            {
                await SerialManager.reader.LoadAsync(1);
            } while (SerialManager.reader.UnconsumedBufferLength < 1);

            if (Controller.SIG_VALIDATE != SerialManager.reader.ReadByte())
                throw new Exception("Fatal Error: master was disconnected and slave was not");

            SerialManager.writer.WriteByte(SIG_CANCEL);
            await SerialManager.writer.StoreAsync();
          
            while (SerialManager.reader.UnconsumedBufferLength > 0)
            {
                SerialManager.reader.ReadByte();
            }

            _inProgress = false;
            
        }

        private async Task handleControllerReset()
        {
            SerialManager.writer.WriteByte(SIG_CONFIRM);
            await SerialManager.writer.StoreAsync();
            SerialManager.writer.WriteBytes(storedBytes);
            await SerialManager.writer.StoreAsync();
            Debug.Write("Sending: ");
            foreach (var tmp in storedBytes)
            {
                Debug.Write(tmp.ToString() + " ");
            }
            Debug.WriteLine("");
        }

        private async Task executeCommands()
        {
            _inProgress = true;
            for (int currentIndex = 0; currentIndex < _currCommands.Count; ++currentIndex)
            {
                SerialManager.writer.WriteByte(SIG_BOM);
                await SerialManager.writer.StoreAsync();
                SerialManager.writer.WriteBytes(_currCommands[currentIndex]);
                await SerialManager.writer.StoreAsync();
                try
                {
                    await serialCommunicate(currentIndex);
                }
                catch (NotSupportedException ex)
                {
                    if (!ex.Message.Contains("Error: Arduino resetted"))
                        throw new Exception("Should not happen!");
                    await handleControllerReset();
                }
            }
            _inProgress = false;
        }

        private async Task receiveState()
        {
            do
            {
                await SerialManager.reader.LoadAsync(24);
            } while (SerialManager.reader.UnconsumedBufferLength < 24);
            byte[] tempBytes = new byte[24];
            SerialManager.reader.ReadBytes(tempBytes);
            foreach (byte b in tempBytes)
            {
                if (b > MAX_PER_BYTE)
                    throw new NotSupportedException("Error: Arduino resetted");
            }
            storedBytes = tempBytes;
            foreach (var tmp in storedBytes)
            {
                Debug.Write(tmp.ToString() + " ");
            }
            Debug.WriteLine("");
        }

        private async Task checkForCancel(int currentIndex)
        {
            if (_cancel == Cancel.PENDING)
            {
                SerialManager.writer.WriteByte(SIG_CANCEL);
                await SerialManager.writer.StoreAsync();
                _cancel = Cancel.DISABLED;
                revertCommands(currentIndex); 
            }
            else
            {
                SerialManager.writer.WriteByte(SIG_CONFIRM);
                await SerialManager.writer.StoreAsync();
            }
        }
        
        private void revertCommands(int currentIndex)
        {
            List<Byte[]> revertedCommands = new List<byte[]>();

            for (int i = currentIndex - 1; i >= 0; --i)
            {
                byte[] command = _currCommands[i];
                byte[] revertedCommand = new byte[4];
                revertedCommand[0] = command[2];
                revertedCommand[1] = command[3];
                revertedCommand[2] = command[0];
                revertedCommand[3] = command[1];
                revertedCommands.Add(revertedCommand);
            }

            _currCommands = revertedCommands;
        }

        /// <summary>
        /// Communicate between Chess engine and motor controller thorugh serial communication
        /// </summary>
        private async Task serialCommunicate(int currentIndex)
        {
                _cancel = Cancel.ENABLED;
                bool EOM = false;
                while (!EOM)
                {
                    do
                    {
                        await SerialManager.reader.LoadAsync(1);
                    } while (SerialManager.reader.UnconsumedBufferLength == 0);
                    byte response = SerialManager.reader.ReadByte();
                    switch (response)
                    {
                        case SIG_SAVE:
                            await receiveState();
                            await checkForCancel(currentIndex);
                            break;
                        case SIG_VALIDATE:
                            throw new NotSupportedException("Error: Arduino resetted");
                        case SIG_EOM:
                            EOM = true;
                            break;
                        default:
                            throw new Exception(response.ToString());
                    }
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
            pos2D[0] = (byte)(_numberOfCells - (pos1D / _numberOfCells));
            // col
            pos2D[1] = (byte)(_numberOfCells -(pos1D % _numberOfCells));
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
                for (int i = 0; i<_deadPieces[tempPiece].Length; ++i)
                {
                    if (_deadPieces[tempPiece][i]._state)
                    {
                        killPiece(_currMove.MovingPiecePrimary.DstPosition,
                            tempPiece);
                        _currCommands.Add(_deadPieces[tempPiece][i]._position.Concat(
                            get2DPos(_currMove.MovingPiecePrimary.DstPosition)).ToArray());
                        _deadPieces[tempPiece][i]._state = false;
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
                for (int i = 0; i < _deadPieces[pieceType].Length; ++i)
                {
                    if (!_deadPieces[pieceType][i]._state)
                    {
                        _currCommands.Add(get2DPos(piecePos).Concat(
                           _deadPieces[pieceType][i]._position).ToArray());
                        _deadPieces[pieceType][i]._state = true;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _deadPawns.Length; ++i)
                {
                    if (!_deadPawns[i]._state)
                    {
                        _currCommands.Add(get2DPos(piecePos).Concat(
                          _deadPawns[i]._position).ToArray());
                        _deadPawns[i]._state = true;
                        break;
                    }
                }
            }
        }
    }
}
