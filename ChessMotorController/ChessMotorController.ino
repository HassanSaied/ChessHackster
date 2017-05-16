#include <Servo.h>
#include "Motor.h"
#include "Utils.h"

// Response handler from master
int response;

// Indicator of movement
bool inProgress;
// Indicator for current position
int currentRow;
int currentColumn;

// To source: Horizontal, Vertical; To Destination: Horizontal, Vertical, Horizontal, Vertical
int moves[6];

// Last saved state indicators
byte lastStateSourceRow, lastStateSourceColumn;
int lastStateMoves[6];

// CNC motors
Motor verticalMotor(200, 3, 2, 4, 5, 6, 14, TOWARDS_MOTOR);
Motor horizontalMotor(200, 9, 8, 10, 11, 12, 15, AGAINST_MOTOR);
Servo magnetServo;

void oneLoopMove()
{
  if ( ( moves[0] == 0 && moves[1] == 0 ) && ( moves[2] != 0 || moves [3] != 0 || moves[4] != 0 || moves[5] != 0 ) )
  {
    magnetServo.write(90);
  }
  else
  {
    magnetServo.write(180);
  }

  // Fast return
  if( !inProgress ) return;

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

}

void waitInput()
{
  byte input[4];
  byte temp, index = 0;
  bool finished = false;

  while(!finished)
  {
    if( Serial.available())
    {
      temp = Serial.read();

      if(temp == SIG_CANCEL)
      {
        index = -1;
      }

      else
      {
        switch (index)
        {
          // BOM signal
          case 0:
            if( temp == SIG_BOM)
            {
              ++index;
            }
            else
            {
              index = -1;
            }
            break;

          default:
            if ( temp <= MAX_PER_BYTE )
            {
              input[index - 1] = temp;
              ++index;
            }
            else
            {
              index = -1;
            }
            break;
        }
      }

      if( index == -1 )
      {
        ++index;
        Serial.write(SIG_EOM);
      }

      else if( index == 5)
      {
        finished = true;
      }

    }
  }

  parseInput(input[0], input[1], input[2], input[3], currentRow, currentColumn, moves, inProgress);
}

void setup() {
  pinMode(LED_BUILTIN,OUTPUT);
  digitalWrite(LED_BUILTIN,LOW);
    // Setting up the motors' configuration
    verticalMotor.setRPM(265);
    verticalMotor.setMicrostep(16);
    horizontalMotor.setRPM(265);
    horizontalMotor.setMicrostep(16);
    magnetServo.attach(16);
    magnetServo.write(180);

    // Set serial
    Serial.begin(9600);

    // Setitng initial position to clear any error
    horizontalMotor.calibrate();
    verticalMotor.calibrate();
    currentRow = 0;
    currentColumn = 0;

    // Initiate
    response = 0;
    do
    {
      Serial.write(SIG_VALIDATE);
      Serial.flush(); // Wait to be sure signal is sent
      delay(10000);
      

      if ( Serial.available() )
      {
        response = Serial.read();
         
      }
    } while ( response != SIG_CONFIRM && response != SIG_CANCEL );

    moves[0] = 0;
    moves[1] = 0;
    moves[2] = 0;
    moves[3] = 0;
    moves[4] = 0;
    moves[5] = 0;
    inProgress = false;
    // Response is either confirm then load, or cancel
    if( response == SIG_CONFIRM)
    {
      // Load last state
      loadState(lastStateSourceRow, lastStateSourceColumn, lastStateMoves);

      // Move to last position
      parseInput( 0, 0,  lastStateSourceRow, lastStateSourceColumn, currentRow, currentColumn, moves, inProgress);

      moves[0] += (moves[2] + moves[4]);
      moves[1] += (moves[3] + moves[5]);
      moves[2] = 0;
      moves[3] = 0;
      moves[4] = 0;
      moves[5] = 0;

     while( moves[0] != 0 || moves[1] != 0 || moves[2] != 0 || moves[3] != 0 || moves[4] != 0 || moves[5] != 0 )
     {
       oneLoopMove();
       if (Serial.available())
       {
        if(Serial.read() == SIG_CANCEL)
        {
         

          memset(moves, 0, sizeof(moves));
          return;
         }
       }
     }

      // Continue last state moves
      memcpy(moves, lastStateMoves, 6*sizeof(int));
      inProgress=true;
    }

}

void loop()
{
    digitalWrite(LED_BUILTIN,HIGH);
    oneLoopMove();

    if (inProgress)
    {
        Serial.write(SIG_SAVE);
        // Save current state
        response = 0;
        while (response != SIG_CONFIRM && response != SIG_CANCEL)
        {
            if (Serial.available())
            {
                response = Serial.read();
            }
        }

        if (response == SIG_CANCEL)
        {
          //TODO 
            /*memset(moves, 0, sizeof(moves));
            inProgress = false;
        */
        }
        else
        {
            saveState(currentRow, currentColumn, moves);
            if (moves[0] == 0 && moves[1] == 0 && moves[2] == 0 && moves[3] == 0 && moves[4] == 0 && moves[5] == 0)
            {
                inProgress = false;
                Serial.write(SIG_EOM);
                
            }
        }
    }
    // Checking for new order
    else
    {
        waitInput();
    }
}

