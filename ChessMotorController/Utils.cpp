#include "Utils.h"

void parseInput(byte sourceRow, byte sourceColumn, byte destinationRow, byte destinationColumn, 
                int& currentRow, int& currentColumn, int* moves, bool& inProgress) 
{
  // Path to source (No magnet attached)
  moves[0] = sourceColumn * BLOCK_SIZE - currentColumn ;
  moves[1] = sourceRow * BLOCK_SIZE - currentRow ;
  
  // Same row
  if ( sourceRow == destinationRow )
  {
    moves[2] = 0;
    moves[3] =  BLOCK_SIZE / 2;
    moves[4] = ( destinationColumn - sourceColumn ) * BLOCK_SIZE;
    moves[5] =  - BLOCK_SIZE / 2 ;
  }
  // Same column
  else if ( sourceColumn == destinationColumn )
  {
    moves[2] =  BLOCK_SIZE / 2;
    moves[3] = ( destinationRow - sourceRow ) * BLOCK_SIZE;
    moves[4] = - BLOCK_SIZE / 2 ;
    moves[5] = 0;
  }
  // Neither
  else
  {
    moves[2] = ( BLOCK_SIZE / 2 ) * ( (destinationColumn > sourceColumn) ? 1 : -1 );
    moves[3] = ( destinationRow - sourceRow ) * BLOCK_SIZE - ( BLOCK_SIZE / 2 ) * ( (destinationRow > sourceRow) ? 1 : -1 ) ;
    moves[4] = ( destinationColumn - sourceColumn ) * BLOCK_SIZE - ( BLOCK_SIZE / 2 ) * ( (destinationColumn > sourceColumn) ? 1 : -1 ) ;
    moves[5] = ( BLOCK_SIZE / 2 ) * ( (destinationRow > sourceRow) ? 1 : -1 );
  }
  
  inProgress = true;
}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


void loadState(int& sourceRow, int& sourceColumn, int& destinationRow, int& destinationColumn, int* moves)
{
  // TODO: implement
}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


void saveState(int row, int col, int* moves)
{
  // TODO: implement
}
