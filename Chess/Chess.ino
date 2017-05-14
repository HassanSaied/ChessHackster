#include <Arduino.h>
#include <Servo.h>
#include "Motor.h"

#define SIG_BEGIN      'b'
#define SIG_FINISH     'f'
#define SIG_SAVE       's'
#define SIG_LOAD       'l'
#define SIG_INITIATE   'i'
#define SIG_CANCEL     'c'

#define BLOCK_SIZE 40

// Indicator of movement
bool inProgress;
// Indicator of magnet
bool magnetUp;
// Indicator for current position
int currentRow;
int currentColumn;

// To source: Horizontal, Vertical; To Destination: Horizontal, Vertical, Horizontal, Vertical
int moves[6];

// CNC motors
Motor verticalMotor(200, 3, 2, 4, 5, 6, 14, TOWARDS_MOTOR);
Motor horizontalMotor(200, 9, 8, 10, 11, 12, 15, AGAINST_MOTOR);
Servo magnetServo;

// Simple parser from byte array given by master
void parseInput(byte sourceRow, byte sourceColumn, byte destinationRow, byte destinationColumn) // 1 1 3 2
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

void setup() {
    // Setting up the motors' configuration
    verticalMotor.setRPM(265);
    verticalMotor.setMicrostep(16);
    horizontalMotor.setRPM(265);
    horizontalMotor.setMicrostep(16);
    magnetServo.attach(16);
    magnetServo.write(180);
    magnetUp = false;

    // Setitng initial position
    horizontalMotor.calibrate();
    verticalMotor.calibrate();

    // TODO: Send load signal and wait for response(moves and last position then go to it)

    // Check for saved state
    inProgress = (moves[0] == 0 && moves[1] == 0 && moves[2] == 0 && moves[3] == 0 && moves[4] == 0 && moves[5] == 0);

}

void loop() {
  if ( ( moves[0] == 0 && moves[1] == 0 ) && ( moves[2] != 0 || moves [3] != 0 || moves[4] != 0 || moves[5] != 0 ) )
  {
    magnetServo.write(90);         
  }
  else
  {
    magnetServo.write(180);
  }

  if (moves[0] != 0)
  {
    if (moves[0] > 0)
    {
      horizontalMotor.moveOneMillimeter(TOWARDS_MOTOR);
      --moves[0];
      ++currentColumn;
    }
    else
    {
      horizontalMotor.moveOneMillimeter(AGAINST_MOTOR);
      ++moves[0];
      --currentColumn;
    }
  }

  else if (moves[1] != 0)
  {
    if (moves[1] > 0)
    {
      verticalMotor.moveOneMillimeter(AGAINST_MOTOR);
      --moves[1];
      ++currentRow;
    }
    else
    {
      verticalMotor.moveOneMillimeter(TOWARDS_MOTOR);
      ++moves[1];
      --currentRow;
    }
  }
  
  else if (moves[2] != 0)
  {
    if (moves[2] > 0)
    {
      horizontalMotor.moveOneMillimeter(TOWARDS_MOTOR);
      --moves[2];
      ++currentColumn;
    }
    else
    {
      horizontalMotor.moveOneMillimeter(AGAINST_MOTOR);
      ++moves[2];
      --currentColumn;
    }
  }
  
  else if (moves[3] != 0)
  {
    if (moves[3] > 0)
    {
      verticalMotor.moveOneMillimeter(AGAINST_MOTOR);
      --moves[3];
      ++currentRow;
    }
    else
    {
      verticalMotor.moveOneMillimeter(TOWARDS_MOTOR);
      ++moves[3];
      --currentRow;
    }
  }

  else if (moves[4] != 0)
  {
    if (moves[4] > 0)
    {
      horizontalMotor.moveOneMillimeter(TOWARDS_MOTOR);
      --moves[4];
      ++currentColumn;
    }
    else
    {
      horizontalMotor.moveOneMillimeter(AGAINST_MOTOR);
      ++moves[4];
      --currentColumn;
    }
  }

  else if (moves[5] != 0)
  {
    if (moves[5] > 0)
    {
     verticalMotor.moveOneMillimeter(AGAINST_MOTOR);
      --moves[5];
      ++currentRow;
    }
    else
    {
      verticalMotor.moveOneMillimeter(TOWARDS_MOTOR);
      ++moves[5];
      --currentRow;
    }
  }

  if(inProgress)
  {
    if(moves[0] == 0 && moves[1] == 0 && moves[2] == 0 && moves[3] == 0 && moves[4] == 0 && moves[5] == 0)
    {
      inProgress = false;
      // TODO: Save final state then begin signal 

    }
    else
    {
      // TODO: Send save signal and moves
    }
  }

  // TODO: Check for cancel signal
}


