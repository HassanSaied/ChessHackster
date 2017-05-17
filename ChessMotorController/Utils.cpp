#include "Utils.h"

void parseInput(byte sourceRow, byte sourceColumn, byte destinationRow, byte destinationColumn,
                int &currentRow, int &currentColumn, int moves[6], bool &inProgress)
{
    // Path to source (No magnet attached)
    moves[0] = sourceColumn * BLOCK_SIZE - currentColumn ;
    moves[1] = sourceRow * BLOCK_SIZE - currentRow ;
    // Same row
    if (sourceRow == destinationRow)
    {
        moves[2] = 0;
        moves[3] =  BLOCK_SIZE / 2;
        moves[4] = (destinationColumn - sourceColumn) * BLOCK_SIZE;
        moves[5] =  - BLOCK_SIZE / 2 ;
    }
    // Same column
    else if (sourceColumn == destinationColumn)
    {
        moves[2] =  BLOCK_SIZE / 2;
        moves[3] = (destinationRow - sourceRow) * BLOCK_SIZE;
        moves[4] = - BLOCK_SIZE / 2 ;
        moves[5] = 0;
    }
    // Neither
    else
    {
        moves[2] = (BLOCK_SIZE / 2) * ((destinationColumn > sourceColumn) ? 1 : -1);
        moves[3] = (destinationRow - sourceRow) * BLOCK_SIZE - (BLOCK_SIZE / 2) * ((destinationRow > sourceRow) ? 1 : -1) ;
        moves[4] = (destinationColumn - sourceColumn) * BLOCK_SIZE - (BLOCK_SIZE / 2) * ((destinationColumn > sourceColumn) ? 1 : -1) ;
        moves[5] = (BLOCK_SIZE / 2) * ((destinationRow > sourceRow) ? 1 : -1);
    }
    inProgress = true;
}

int receiveTwoByteNumber()
{
    return ((Serial.read() == 1)? -1 : 1) * (Serial.read() * MAX_PER_BYTE + Serial.read());
}

void loadState(int moves[6])
{
    while (Serial.available() < 24);
    int currentRow = receiveTwoByteNumber();
    int currentColumn = receiveTwoByteNumber();
    for (int i = 0; i < 6; ++i)
    {
        moves[i] = receiveTwoByteNumber();
    }
    moves[0] += currentColumn;
    moves[1] += currentRow;
}

void sendTwoByteNumber(int twoByteNumber)
{
    if (twoByteNumber < 0)
    {
        Serial.write(1);
        Serial.flush();
        twoByteNumber = - twoByteNumber;
    }
    else
    {
        Serial.write(0);
        Serial.flush();
    }
    Serial.write(twoByteNumber / MAX_PER_BYTE);
    Serial.flush();
    Serial.write(twoByteNumber % MAX_PER_BYTE);
    Serial.flush();
}

void saveState(int row, int column, int moves[6])
{
    sendTwoByteNumber(row);
    sendTwoByteNumber(column);
    for (int i = 0; i < 6; ++i)
    {
        sendTwoByteNumber(moves[i]);
    }
}

