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
bool cancel;
int moves[6];
int inProgressMoves[4];

// CNC motors
Motor verticalMotor(200, 3, 2, 4, 5, 6, 14, TOWARDS_MOTOR);
Motor horizontalMotor(200, 9, 8, 10, 11, 12, 15, AGAINST_MOTOR);
Servo magnetServo;

void moveHorizontally(int& move)
{
    if (move > 0)
    {
        horizontalMotor.moveOneMillimeter(TOWARDS_MOTOR);
        --move;
        ++currentColumn;
    }
    else
    {
        horizontalMotor.moveOneMillimeter(AGAINST_MOTOR);
        ++move;
        --currentColumn;
    }
}

void moveVertically(int& move)
{
    if (move > 0)
    {
        verticalMotor.moveOneMillimeter(AGAINST_MOTOR);
        --move;
        ++currentRow;
    }
    else
    {
        verticalMotor.moveOneMillimeter(TOWARDS_MOTOR);
        ++move;
        --currentRow;
    }
}

void oneLoopMove()
{
    if ((moves[0] == 0 && moves[1] == 0) && (moves[2] != 0 || moves [3] != 0 || moves[4] != 0 || moves[5] != 0))
    {
        magnetServo.write(90);
    }
    else
    {
        magnetServo.write(180);
    }
    
    // Fast return
    if (!inProgress)
    {
        return;
    }

    byte cancelIndicator = (cancel) ? 1 : 0;
    for (int i = 0; i < 6; i++)
    {
        if (moves[i] != 0)
        {
            (i % 2 == cancelIndicator) ?  moveHorizontally(moves[i]) : moveVertically(moves[i]);
            break;
        }
    }
}

void saveState()
{
    Serial.write(SIG_SAVE);
    // Save current state
    saveState(currentRow, currentColumn, moves);
    if (moves[0] == 0 && moves[1] == 0 && moves[2] == 0 && moves[3] == 0 && moves[4] == 0 && moves[5] == 0)
    {
        inProgress = false;
        Serial.write(SIG_EOM);
    }
}

void checkForCancel()
{
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
        cancel = true;
        if (moves[0] != 0 || moves[1] != 0)
        {
            moves[0] = 0;
            moves[1] = 0;
            moves[2] = 0;
            moves[3] = 0;
            moves[4] = 0;
            moves[5] = 0;
        }
        else
        {
            int temp[6];
            memcpy(temp, moves, 6 * sizeof(moves[0]));
            moves[5] = temp[2] - inProgressMoves[0];
            moves[4] = temp[3] - inProgressMoves[1];
            moves[3] = temp[4] - inProgressMoves[2];
            moves[2] = temp[5] - inProgressMoves[3];
        }
    }
}

void waitInput()
{
    byte input[4];
    byte temp, index = 0;
    bool finished = false;
    while (!finished)
    {
        if (Serial.available())
        {
            temp = Serial.read();
            switch (index)
            {
              // BOM signal
                case 0:
                    if (temp == SIG_BOM)
                    {
                      ++index;
                    }
                    break;
                default:
                    input[index - 1] = temp;
                    ++index;
                    break;
            }
        }
        if (index == 5)
          finished = true;
    }
    parseInput(input[0], input[1], input[2], input[3], currentRow, currentColumn, moves, inProgress);
    inProgressMoves[0] = moves[2];
    inProgressMoves[1] = moves[3];
    inProgressMoves[2] = moves[4];
    inProgressMoves[3] = moves[5];
    cancel = false;
}

void setup()
{
    // For debugging
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, LOW);
    // Setting up the motors' configuration
    verticalMotor.setRPM(265);
    verticalMotor.setMicrostep(16);
    horizontalMotor.setRPM(265);
    horizontalMotor.setMicrostep(16);
    magnetServo.attach(16);
    magnetServo.write(180);
    // Set serial
    Serial.begin(9600);
    // Setting initial position to clear any error
    horizontalMotor.calibrate();
    verticalMotor.calibrate();
    currentRow = 0;
    currentColumn = 0;
    cancel = false;
    // Initiate
    response = 0;
    do
    {
        Serial.write(SIG_VALIDATE);
        Serial.flush(); // Wait to be sure signal is sent
        delay(10000);
        if (Serial.available())
        {
            response = Serial.read();
        }
    }
    while (response != SIG_CONFIRM && response != SIG_CANCEL);
    // Response is either confirm then load, or cancel
    if (response == SIG_CONFIRM)
    {
        loadState(moves);
        if (moves[0] == 0 && moves[1] == 0 && moves[2] == 0 && moves[3] == 0 && moves[4] == 0 && moves[5] == 0)
            inProgress = false;
        else
            inProgress = true;
    }
    else // response == SIG_CANCEL
    {
        moves[0] = 0;
        moves[1] = 0;
        moves[2] = 0;
        moves[3] = 0;
        moves[4] = 0;
        moves[5] = 0;
        inProgress = false;
    }
}

void loop()
{
    // For debugging
    digitalWrite(LED_BUILTIN, HIGH);
    
    // Perform one move
    oneLoopMove();
    
    if (inProgress)
    {
        saveState();
        checkForCancel();
    }
    else
    {
        waitInput();
    }
}


