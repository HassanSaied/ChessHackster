#pragma once
#include "A4988.h"
				
typedef bool Direction;
				
#define FULL_CYCLE 360

#define AGAINST_MOTOR true
#define TOWARDS_MOTOR false

// using a 200-step motor (most common)
// pins used are DIR, STEP, MS1, MS2, MS3 in that order
class Motor: public A4988
{
  private:
    const int calibrationPin; 
    Direction calibrationDirection;
  public:

    Motor(int steps, int dir_pin, int step_pin, int ms1_pin, int ms2_pin, int ms3_pin, 
		      int calibrationPin, Direction calibrationDirection);
    
    void moveOneMillimeter(Direction directionFlag);
    
    // Move to a fixed reference point
    void calibrate();
};
